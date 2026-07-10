using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Pokekool.Application.Collection.Scanning;
using Pokekool.Shared.Dtos.Collection;

namespace Pokekool.Infrastructure.Collection.Scanning;

/// <summary>
/// Lit et met à jour la base de connaissance RAG stockée dans ai-brain/scan-knowledge/
/// (chemin relatif au ContentRootPath, configuré via "AiBrainPath").
/// Résilient : si le dossier n'existe pas, retourne un contexte vide et logue.
/// </summary>
public sealed class VaultKnowledgeService : IVaultKnowledgeService, IDisposable
{
    private const int RecentCardsCount = 15;

    private static readonly string[] HintFiles =
    {
        "grading-slabs.md",
        "set-symbols.md",
        "card-anatomy.md"
    };

    private readonly string _vaultPath;
    private readonly ILogger<VaultKnowledgeService> _logger;
    private readonly SemaphoreSlim _writeLock = new(1, 1);

    public VaultKnowledgeService(
        IConfiguration configuration,
        IWebHostEnvironment env,
        ILogger<VaultKnowledgeService> logger)
    {
        var brainRelative = configuration["AiBrainPath"] ?? "ai-brain";
        var brainRoot     = Path.IsPathRooted(brainRelative)
            ? brainRelative
            : Path.Combine(env.ContentRootPath, brainRelative);
        _vaultPath = Path.Combine(brainRoot, "scan-knowledge");
        _logger    = logger;
    }

    public void Dispose() => _writeLock.Dispose();

    private string CardsDir => Path.Combine(_vaultPath, "cards");
    private string HintsDir => Path.Combine(_vaultPath, "hints");
    private string StatsFile => Path.Combine(_vaultPath, "stats.md");

    public async Task<string> BuildPromptContextAsync(CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_vaultPath) || !Directory.Exists(_vaultPath))
        {
            _logger.LogWarning("ai-brain/scan-knowledge introuvable ('{Path}') — contexte RAG vide", _vaultPath);
            return string.Empty;
        }

        var sb = new StringBuilder();
        sb.AppendLine("=== HINTS VISUELS ===");

        foreach (var hint in HintFiles)
        {
            var path = Path.Combine(HintsDir, hint);
            if (File.Exists(path))
            {
                sb.AppendLine(await File.ReadAllTextAsync(path, ct));
                sb.AppendLine();
            }
        }

        sb.AppendLine($"=== CARTES CONNUES DU CATALOGUE ({RecentCardsCount} plus récentes) ===");

        foreach (var line in await BuildKnownCardLinesAsync(ct))
            sb.AppendLine(line);

        return sb.ToString();
    }

    private async Task<IReadOnlyList<string>> BuildKnownCardLinesAsync(CancellationToken ct)
    {
        if (!Directory.Exists(CardsDir))
            return Array.Empty<string>();

        var files = Directory.EnumerateFiles(CardsDir, "*.md").ToList();
        var entries = new List<(DateTime LastSeen, string Line)>();

        foreach (var file in files)
        {
            var content = await File.ReadAllTextAsync(file, ct);
            var (fm, body) = SplitFrontmatter(content);

            var name = fm.GetValueOrDefault("name");
            if (string.IsNullOrWhiteSpace(name))
                continue;

            var set = fm.GetValueOrDefault("set");
            var number = fm.GetValueOrDefault("number");
            var avg = fm.GetValueOrDefault("avgConfidence");
            var hints = CollapseSection(body, "## Indices visuels confirmés");

            var lastSeen = DateTime.TryParse(
                fm.GetValueOrDefault("lastSeen"), CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var ls)
                ? ls
                : File.GetLastWriteTimeUtc(file);

            var line = $"- {name} · {set} · {number} (confiance moy: {avg}) | Indices: {hints}";
            entries.Add((lastSeen, line));
        }

        return entries
            .OrderByDescending(e => e.LastSeen)
            .Take(RecentCardsCount)
            .Select(e => e.Line)
            .ToList();
    }

    public async Task UpdateCardEntryAsync(
        CardScanMatchDto card,
        double confirmedConfidence,
        CancellationToken ct = default)
    {
        if (card.CardId is null || string.IsNullOrWhiteSpace(_vaultPath))
            return;

        await _writeLock.WaitAsync(ct);
        try
        {
            Directory.CreateDirectory(CardsDir);

            var slug = Slugify(card.CardName);
            var path = Path.Combine(CardsDir, $"{card.CardId}-{slug}.md");
            var today = DateTimeOffset.UtcNow.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

            if (File.Exists(path))
            {
                var content = await File.ReadAllTextAsync(path, ct);
                var (fm, body) = SplitFrontmatter(content);

                var scanCount = ParseInt(fm.GetValueOrDefault("scanCount")) + 1;
                var prevAvg = ParseDouble(fm.GetValueOrDefault("avgConfidence"));
                var avg = ((prevAvg * (scanCount - 1)) + confirmedConfidence) / scanCount;

                fm["scanCount"] = scanCount.ToString(CultureInfo.InvariantCulture);
                fm["avgConfidence"] = Math.Round(avg, 2).ToString(CultureInfo.InvariantCulture);
                fm["lastSeen"] = today;

                await File.WriteAllTextAsync(path, BuildFrontmatter(fm) + body, ct);
            }
            else
            {
                await File.WriteAllTextAsync(path, BuildNewCardFile(card, confirmedConfidence, today), ct);
            }
        }
        finally
        {
            _writeLock.Release();
        }
    }

    public async Task UpdateStatsAsync(int scanned, int confirmed, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_vaultPath) || !Directory.Exists(_vaultPath))
            return;

        await _writeLock.WaitAsync(ct);
        try
        {
            var totalScans = 0;
            var totalConfirmed = 0;

            if (File.Exists(StatsFile))
            {
                var (fm, _) = SplitFrontmatter(await File.ReadAllTextAsync(StatsFile, ct));
                totalScans = ParseInt(fm.GetValueOrDefault("totalScans"));
                totalConfirmed = ParseInt(fm.GetValueOrDefault("totalConfirmed"));
            }

            totalScans += scanned;
            totalConfirmed += confirmed;
            var lastScan = DateTimeOffset.UtcNow.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

            await File.WriteAllTextAsync(StatsFile, BuildStatsFile(totalScans, totalConfirmed, lastScan), ct);
        }
        finally
        {
            _writeLock.Release();
        }
    }

    // ── Helpers ────────────────────────────────────────────────────────────

    private static (Dictionary<string, string> Frontmatter, string Body) SplitFrontmatter(string content)
    {
        var fm = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var normalized = content.Replace("\r\n", "\n", StringComparison.Ordinal);

        if (!normalized.StartsWith("---\n", StringComparison.Ordinal))
            return (fm, content);

        var end = normalized.IndexOf("\n---", 4, StringComparison.Ordinal);
        if (end < 0)
            return (fm, content);

        var block = normalized[4..end];
        foreach (var raw in block.Split('\n'))
        {
            var idx = raw.IndexOf(':', StringComparison.Ordinal);
            if (idx <= 0)
                continue;

            var key = raw[..idx].Trim();
            var value = raw[(idx + 1)..].Trim();
            if (key.Length > 0)
                fm[key] = value;
        }

        // Corps après le second '---' (et le retour à la ligne suivant).
        var afterEnd = end + "\n---".Length;
        var body = afterEnd < normalized.Length ? normalized[afterEnd..] : string.Empty;
        if (body.StartsWith('\n'))
            body = body[1..];

        return (fm, body);
    }

    private static string BuildFrontmatter(Dictionary<string, string> fm)
    {
        var sb = new StringBuilder();
        sb.Append("---\n");
        foreach (var kv in fm)
            sb.Append(kv.Key).Append(": ").Append(kv.Value).Append('\n');
        sb.Append("---\n\n");
        return sb.ToString();
    }

    private static string CollapseSection(string body, string heading)
    {
        var normalized = body.Replace("\r\n", "\n", StringComparison.Ordinal);
        var start = normalized.IndexOf(heading, StringComparison.Ordinal);
        if (start < 0)
            return string.Empty;

        start += heading.Length;
        var next = normalized.IndexOf("\n## ", start, StringComparison.Ordinal);
        var section = next < 0 ? normalized[start..] : normalized[start..next];

        var collapsed = section
            .Replace('\n', ' ')
            .Replace("*", string.Empty, StringComparison.Ordinal)
            .Trim();

        // Ignore le texte de remplissage par défaut.
        return collapsed.StartsWith("(À enrichir", StringComparison.Ordinal) ? string.Empty : collapsed;
    }

    private static string BuildNewCardFile(CardScanMatchDto card, double confidence, string today)
    {
        var avg = Math.Round(confidence, 2).ToString(CultureInfo.InvariantCulture);
        return $"""
            ---
            cardId: {card.CardId}
            name: {card.CardName}
            set: {card.SetName}
            number: {card.CardNumber}
            scanCount: 1
            avgConfidence: {avg}
            lastSeen: {today}
            ---

            ## Indices visuels confirmés
            *(À enrichir — le LLM n'a pas encore fourni de détails visuels sur cette carte)*

            ## Grading observé
            *(Aucune observation de slab pour l'instant)*

            ## Confusions passées
            *(Aucune)*

            """;
    }

    private static string BuildStatsFile(int totalScans, int totalConfirmed, string lastScan)
    {
        var accuracy = totalScans > 0
            ? $"{Math.Round(100.0 * totalConfirmed / totalScans, 1).ToString(CultureInfo.InvariantCulture)} %"
            : "—";

        return $"""
            ---
            totalScans: {totalScans}
            totalConfirmed: {totalConfirmed}
            lastScan: {lastScan}
            ---

            # Statistiques globales — Scan Collection

            Ce fichier est mis à jour automatiquement par le backend après chaque session de scan confirmée.

            | Champ           | Valeur |
            |-----------------|--------|
            | Total scans     | {totalScans}      |
            | Total confirmés | {totalConfirmed}      |
            | Dernier scan    | {lastScan}      |
            | Précision       | {accuracy}      |

            > **Note** : le taux de précision (confirmed / scanned) est un indicateur de la qualité du modèle LLM Vision et de la richesse du Vault.

            """;
    }

    private static string Slugify(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return "card";

        return name.ToLowerInvariant()
            .Replace(" ", "-", StringComparison.Ordinal)
            .Replace("/", "-", StringComparison.Ordinal);
    }

    private static int ParseInt(string? value) =>
        int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var i) ? i : 0;

    private static double ParseDouble(string? value) =>
        double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var d) ? d : 0d;
}
