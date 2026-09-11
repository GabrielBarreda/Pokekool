using Pokekool.Domain.Entities.Collection;

namespace Pokekool.Application.Collection;

/// <summary>Conversions string &lt;-&gt; enums du module Scan (tolérantes à la casse) — voir
/// StartScanRunRequest côté DTO partagé (Pokekool.Shared n'a aucune dépendance framework, donc
/// pas de référence directe aux enums Domain).</summary>
public static class ScanEnumMap
{
    public static ScanDestinationType ParseDestinationType(string value)
        => Enum.TryParse<ScanDestinationType>(value, ignoreCase: true, out var v)
            ? v : throw ScanErrors.InvalidEnum("destinationType", value);

    public static ScanReason ParseReason(string value)
        => Enum.TryParse<ScanReason>(value, ignoreCase: true, out var v)
            ? v : throw ScanErrors.InvalidEnum("reason", value);

    public static ScanReason? ParseReasonOrNull(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : ParseReason(value);
}
