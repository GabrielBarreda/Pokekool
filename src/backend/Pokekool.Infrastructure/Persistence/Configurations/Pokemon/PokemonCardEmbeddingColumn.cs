namespace Pokekool.Infrastructure.Persistence.Configurations.Pokemon;

/// <summary>
/// Constantes partagées pour la shadow property EF Core portant l'embedding visuel (DINOv2)
/// de PokemonCard. Type Pgvector.Vector défini uniquement ici, en Infrastructure — le Domain
/// (IPokemonCardRepository) ne connaît que des float[], jamais Vector (règle cardinale du
/// Domain : zéro dépendance framework/ORM, voir Pokekool.Domain.csproj).
/// Partagé entre PokemonCardConfiguration (mapping) et PokemonCardRepository (requêtes) pour
/// éviter que le nom de la shadow property diverge entre les deux.
/// </summary>
internal static class PokemonCardEmbeddingColumn
{
    public const string ShadowPropertyName = "Embedding";
    public const string ColumnName = "embedding";

    /// <summary>Dimension du vecteur DINOv2 (modèle dinov2_vits14) — voir services/card-embedding/app.py.</summary>
    public const int Dimensions = 384;
}
