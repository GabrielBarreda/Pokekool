using Pokekool.Domain.Common;

namespace Pokekool.Domain.Entities.Collection;

public static class ScanErrors
{
    public static DomainException InvalidEnum(string field, string value)
        => new("scan.invalid_enum", $"Valeur invalide '{value}' pour '{field}'.");

    public static DomainException RunNotFound(Guid id)
        => new("scan_run.not_found", $"ScanRun '{id}' introuvable.");
}
