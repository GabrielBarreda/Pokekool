namespace Pokekool.Application.Abstractions;

/// <summary>
/// Abstraction du temps. Permet de mocker en tests.
/// </summary>
public interface IDateTimeProvider
{
    DateTimeOffset UtcNow { get; }
}
