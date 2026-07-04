namespace Pokekool.Shared.Dtos.Pokemon;

public sealed record CardGradingDto(
    string   Company,
    DateTime LastUpdated,
    IReadOnlyList<GradeEntryDto> Entries);

public sealed record GradeEntryDto(
    decimal  Grade,
    string   Label,
    decimal? MarketPrice,
    decimal? LowPrice,
    int?     Population,
    string?  Source   = null,
    string?  Currency = null);
