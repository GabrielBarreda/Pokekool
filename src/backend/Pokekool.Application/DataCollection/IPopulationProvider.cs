namespace Pokekool.Application.DataCollection;

public interface IPopulationProvider : IDataProvider
{
    /// <summary>
    /// Retourne les données de population pour une carte donnée.
    /// Peut retourner plusieurs entrées (une par société de notation, ex : PSA + BGS).
    /// </summary>
    Task<IReadOnlyList<CardPopulationData>?> GetPopulationAsync(
        CardPopulationRequest request, CancellationToken ct = default);
}
