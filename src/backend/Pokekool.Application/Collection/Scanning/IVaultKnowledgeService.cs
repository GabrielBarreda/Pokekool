using Pokekool.Shared.Dtos.Collection;

namespace Pokekool.Application.Collection.Scanning;

/// <summary>
/// Accès à la base de connaissance RAG (Vault Obsidian) pour enrichir le prompt LLM Vision
/// et persister les indices visuels après confirmation utilisateur.
/// </summary>
public interface IVaultKnowledgeService
{
    /// <summary>Construit le bloc de contexte RAG à injecter dans le prompt LLM.</summary>
    Task<string> BuildPromptContextAsync(CancellationToken ct = default);

    /// <summary>Crée ou met à jour la fiche d'une carte confirmée dans le Vault.</summary>
    Task UpdateCardEntryAsync(
        CardScanMatchDto card,
        double confirmedConfidence,
        CancellationToken ct = default);

    /// <summary>Met à jour stats.md (totalScans, totalConfirmed, lastScan).</summary>
    Task UpdateStatsAsync(int scanned, int confirmed, CancellationToken ct = default);
}
