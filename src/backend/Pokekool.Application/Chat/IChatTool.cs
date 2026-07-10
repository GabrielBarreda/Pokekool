namespace Pokekool.Application.Chat;

/// <summary>Outil appelable par le LLM via function calling.</summary>
public interface IChatTool
{
    string Name { get; }
    string Description { get; }
    object ParametersSchema { get; }  // objet JSON Schema sérialisé

    /// <summary>Exécute l'outil. Retourne un string (résultat lisible par le LLM).</summary>
    Task<string> ExecuteAsync(
        IDictionary<string, object?> args,
        ChatSession session,
        CancellationToken ct = default);
}
