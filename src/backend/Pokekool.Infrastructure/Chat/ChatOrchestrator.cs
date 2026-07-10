using System.Text.Json;
using Microsoft.Extensions.Logging;
using Pokekool.Application.Chat;
using Pokekool.Shared.Dtos.Chat;
using Pokekool.Infrastructure.Chat.Tools;

namespace Pokekool.Infrastructure.Chat;

/// <summary>
/// Cœur du chatbot : boucle ReAct (Reason + Act), human-in-the-loop pour les actions,
/// et directives client (ex: redirect caméra).
/// </summary>
public sealed class ChatOrchestrator : IChatOrchestrator
{
    private const int MaxToolCallRounds = 5;

    private static readonly HashSet<string> ConfirmKeywords = new(StringComparer.OrdinalIgnoreCase)
    { "oui", "yes", "ok", "confirme", "confirmer", "go", "valide", "c'est bon", "parfait", "fait" };

    private static readonly HashSet<string> ActionTools = new(StringComparer.OrdinalIgnoreCase)
    { "create_stock_entry", "create_shop_product", "update_appointment_status", "reschedule_appointment" };

    private readonly IOllamaChatClient _ollama;
    private readonly RedisChatSessionStore _sessions;
    private readonly IReadOnlyDictionary<string, IChatTool> _tools;
    private readonly ILogger<ChatOrchestrator> _logger;

    public ChatOrchestrator(
        IOllamaChatClient ollama,
        RedisChatSessionStore sessions,
        IEnumerable<IChatTool> tools,
        ILogger<ChatOrchestrator> logger)
    {
        _ollama = ollama;
        _sessions = sessions;
        _tools = tools.ToDictionary(t => t.Name, StringComparer.OrdinalIgnoreCase);
        _logger = logger;
    }

    public async Task<ChatResponseDto> ProcessAsync(ChatRequestDto request, CancellationToken ct)
    {
        // 1 — Charger ou créer session.
        ChatSession session;
        if (!string.IsNullOrWhiteSpace(request.SessionId))
            session = await _sessions.GetAsync(request.SessionId) ?? CreateNewSession();
        else
            session = CreateNewSession();

        // 2 — L'utilisateur confirme une action en attente ?
        if (session.PendingAction is not null && IsConfirmation(request.Message))
        {
            var (replyText, confirmDirective) = await ExecutePendingActionAsync(session, ct);
            session.AddAssistantMessage(replyText);
            session.PendingAction = null;
            await _sessions.SaveAsync(session);
            return new ChatResponseDto(session.Id, replyText, null, confirmDirective);
        }

        // 3 — Annulation en attente.
        if (session.PendingAction is not null && IsCancellation(request.Message))
        {
            session.PendingAction = null;
            const string cancelMsg = "Action annulée.";
            session.AddAssistantMessage(cancelMsg);
            await _sessions.SaveAsync(session);
            return new ChatResponseDto(session.Id, cancelMsg, null, null);
        }

        // 4 — L'utilisateur change de sujet : on efface l'ancienne action en attente.
        session.PendingAction = null;

        // 5 — Ajouter l'image en contexte si présente (produit scellé / UPC).
        var userMessage = request.Message;
        if (!string.IsNullOrWhiteSpace(request.ImageBase64))
            userMessage = $"[IMAGE FOURNIE — identifier le produit]\n{request.Message}";

        session.AddUserMessage(userMessage);

        // 6 — Boucle ReAct.
        var toolDefs = _tools.Values
            .Select(t => new OllamaToolDefinition(t.Name, t.Description, t.ParametersSchema))
            .ToList();

        ChatPendingAction? pendingAction = null;
        ChatClientDirective? directive = null;
        string finalMessage = string.Empty;

        for (int round = 0; round < MaxToolCallRounds; round++)
        {
            var messages = BuildSystemMessages().Concat(session.GetRecentHistory()).ToList();
            var response = await _ollama.SendAsync(messages, toolDefs, ct);

            if (response.ToolCalls.Count == 0)
            {
                finalMessage = string.IsNullOrWhiteSpace(response.Content)
                    ? "Je n'ai pas compris. Pouvez-vous reformuler ?"
                    : response.Content!;
                break;
            }

            bool stop = false;
            foreach (var call in response.ToolCalls)
            {
                if (ActionTools.Contains(call.FunctionName))
                {
                    // Human-in-the-loop : construire PendingAction et s'arrêter.
                    var summary = BuildActionSummary(call.FunctionName, call.Arguments);
                    pendingAction = new ChatPendingAction(
                        ActionId: Guid.NewGuid().ToString("N"),
                        Summary: summary,
                        ToolName: call.FunctionName,
                        ArgsJson: JsonSerializer.Serialize(call.Arguments));
                    session.PendingAction = pendingAction;
                    finalMessage = $"{summary}\n\nConfirmer cette action ? (oui / non)";
                    stop = true;
                    break;
                }

                if (!_tools.TryGetValue(call.FunctionName, out var tool))
                {
                    _logger.LogWarning("Outil inconnu appelé par le LLM : {Tool}", call.FunctionName);
                    session.AddToolResult(call.FunctionName, "Outil non trouvé.");
                    continue;
                }

                var toolResult = await tool.ExecuteAsync(call.Arguments, session, ct);

                // Détecter la directive redirect_camera.
                if (toolResult.StartsWith("__redirect_camera:", StringComparison.Ordinal))
                {
                    var purpose = toolResult["__redirect_camera:".Length..];
                    directive = new ChatClientDirective("redirect_camera", $"/admin/collection/scan?purpose={purpose}");
                    finalMessage = "Ouverture de la caméra…";
                    stop = true;
                    break;
                }

                session.AddToolResult(call.FunctionName, toolResult);
            }

            if (stop) break;
        }

        if (string.IsNullOrEmpty(finalMessage))
            finalMessage = "Je n'ai pas pu aboutir. Pouvez-vous préciser votre demande ?";

        session.AddAssistantMessage(finalMessage);
        await _sessions.SaveAsync(session);
        return new ChatResponseDto(session.Id, finalMessage, pendingAction, directive);
    }

    private async Task<(string, ChatClientDirective?)> ExecutePendingActionAsync(ChatSession session, CancellationToken ct)
    {
        var pending = session.PendingAction!;
        if (!_tools.TryGetValue(pending.ToolName, out var tool))
            return ("Outil introuvable — action impossible.", null);

        var args = JsonSerializer.Deserialize<Dictionary<string, object?>>(pending.ArgsJson)
                   ?? new Dictionary<string, object?>();

        try
        {
            var result = await tool.ExecuteAsync(args, session, ct);
            return (result, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur exécution outil {Tool}", pending.ToolName);
            return ($"Erreur lors de l'exécution : {ex.Message}", null);
        }
    }

    private static IEnumerable<OllamaChatMessage> BuildSystemMessages()
    {
        yield return new OllamaChatMessage("system",
            """
            Tu es l'assistant IA de Pokekool, une boutique de cartes Pokémon.
            Tu aides l'administrateur et les employés à gérer le stock, le présentoir et le planning.

            Règles :
            - Réponds TOUJOURS en français.
            - Avant toute action irréversible (ajout stock, mise en vente, modification RDV), résume ce que tu vas faire et attends "oui".
            - Pour les ambiguïtés (plusieurs cartes du même nom), utilise search_shop_stock ou search_shop_products pour lister les variantes et demande laquelle choisir.
            - Pour les prix marché, utilise get_market_price.
            - Pour photographier un produit, utilise open_camera.
            - Sois concis : une réponse, un appel d'outil à la fois si possible.

            L'utilisateur parle français. Traduis les états de carte en toutes lettres vers le code technique
            attendu par les outils :
            - "neuf" / "comme neuf" / "nickel" -> NM
            - "très bon" / "très bon état" -> LP
            - "bon" / "bon état" -> MP
            - "usé" / "abîmé léger" / "joué" -> HP
            - "abîmé" / "endommagé" / "mauvais état" -> DMG
            Dans tes réponses à l'utilisateur, affiche toujours le libellé français (Neuf, Très bon, Bon, Usé, Abîmé),
            jamais le code ni de terme anglais (pas de "Mint", "NM", "HP"...).
            """);
    }

    private static readonly IReadOnlyDictionary<string, string> ConditionFr =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        { ["NM"] = "Neuf", ["LP"] = "Très bon", ["MP"] = "Bon", ["HP"] = "Usé", ["DMG"] = "Abîmé" };

    /// <summary>Libellé FR d'un code d'état (ou le code tel quel si inconnu).</summary>
    private static string FrCondition(object? code, string fallback = "")
    {
        var s = code?.ToString();
        if (string.IsNullOrEmpty(s)) return fallback;
        return ConditionFr.TryGetValue(s, out var fr) ? fr : s;
    }

    private static string BuildActionSummary(string toolName, IDictionary<string, object?> args)
        => toolName switch
        {
            "create_stock_entry" => $"➕ Ajouter au stock : {args.GetValueOrDefault("productName")} × {args.GetValueOrDefault("quantityTotal")} (état: {FrCondition(args.GetValueOrDefault("condition"), "Neuf")}, prix achat: {args.GetValueOrDefault("purchasePrice") ?? "—"} €)",
            "create_shop_product" => $"🏪 Mettre en vente : {args.GetValueOrDefault("productName")} × {args.GetValueOrDefault("quantity")} à {args.GetValueOrDefault("price")} €",
            "update_appointment_status" => $"📅 RDV {args.GetValueOrDefault("appointmentId")} → statut '{args.GetValueOrDefault("status")}'",
            "reschedule_appointment" => $"📅 Déplacer RDV {args.GetValueOrDefault("appointmentId")} au {args.GetValueOrDefault("newScheduledAt")}",
            _ => $"Action : {toolName} ({JsonSerializer.Serialize(args)})"
        };

    private static ChatSession CreateNewSession() => new();

    private static bool IsConfirmation(string msg)
        => ConfirmKeywords.Any(k => msg.Trim().Equals(k, StringComparison.OrdinalIgnoreCase)
                                 || msg.Trim().StartsWith(k + " ", StringComparison.OrdinalIgnoreCase));

    private static bool IsCancellation(string msg)
    {
        var lower = msg.Trim().ToLowerInvariant();
        return lower is "non" or "no" or "annule" or "annuler" or "stop" or "cancel";
    }
}
