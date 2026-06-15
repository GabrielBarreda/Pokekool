namespace Pokekool.Domain.Common;

/// <summary>
/// Exception levée par les règles métier du domaine. Mappée en 422/400 côté API.
/// </summary>
public class DomainException : Exception
{
    public string Code { get; }

    public DomainException(string code, string message) : base(message)
        => Code = code;
}
