namespace Pokekool.Shared.Dtos.Shop;

public sealed record AvailableSlotDto(
    string Date,   // YYYY-MM-DD
    string Time,   // HH:mm
    bool Available);
