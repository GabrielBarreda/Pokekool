namespace Pokekool.Application.DataCollection;

/// <summary>
/// Exception levée par un provider quand il ne peut pas récupérer les données.
/// L'Aggregator la capture, log l'erreur, et continue avec les autres providers.
/// </summary>
public sealed class ProviderException : Exception
{
    public string ProviderName { get; }

    public ProviderException(string providerName, string message, Exception? inner = null)
        : base(message, inner)
    {
        ProviderName = providerName;
    }
}
