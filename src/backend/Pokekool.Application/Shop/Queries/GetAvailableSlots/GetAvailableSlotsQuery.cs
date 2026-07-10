using System.Text.Json;
using System.Text.Json.Serialization;
using MediatR;
using Pokekool.Domain.Repositories;
using Pokekool.Shared.Dtos.Shop;

namespace Pokekool.Application.Shop.Queries.GetAvailableSlots;

/// <summary>
/// Calcule les créneaux disponibles pour un service et une date donnés.
/// Intervalles de DurationMinutes dans les horaires d'ouverture (Shop.hours),
/// en excluant les créneaux déjà occupés.
/// </summary>
public sealed record GetAvailableSlotsQuery(string ServiceType, string Date) : IRequest<IReadOnlyList<AvailableSlotDto>>;

public sealed class GetAvailableSlotsQueryHandler
    : IRequestHandler<GetAvailableSlotsQuery, IReadOnlyList<AvailableSlotDto>>
{
    private const int DefaultDurationMinutes = 60;

    private static readonly string[] FrenchDays =
        { "dimanche", "lundi", "mardi", "mercredi", "jeudi", "vendredi", "samedi" };

    private static readonly JsonSerializerOptions HoursJsonOptions =
        new() { PropertyNameCaseInsensitive = true };

    private readonly IShopClientRepository _repo;

    public GetAvailableSlotsQueryHandler(IShopClientRepository repo) => _repo = repo;

    public async Task<IReadOnlyList<AvailableSlotDto>> Handle(GetAvailableSlotsQuery request, CancellationToken cancellationToken)
    {
        if (!DateOnly.TryParse(request.Date, out var date))
            return Array.Empty<AvailableSlotDto>();

        var shop = await _repo.GetShopHoursAsync(cancellationToken);
        if (shop is null || string.IsNullOrWhiteSpace(shop.HoursJson))
            return Array.Empty<AvailableSlotDto>();

        Dictionary<string, DayHours>? hours;
        try
        {
            hours = JsonSerializer.Deserialize<Dictionary<string, DayHours>>(shop.HoursJson,
                HoursJsonOptions);
        }
        catch (JsonException)
        {
            return Array.Empty<AvailableSlotDto>();
        }

        if (hours is null) return Array.Empty<AvailableSlotDto>();

        var dayKey = FrenchDays[(int)date.DayOfWeek];
        if (!hours.TryGetValue(dayKey, out var day) || day is null || day.Closed)
            return Array.Empty<AvailableSlotDto>();

        if (!TimeOnly.TryParse(day.Open, out var open) || !TimeOnly.TryParse(day.Close, out var close))
            return Array.Empty<AvailableSlotDto>();

        var booked = await _repo.GetBookedSlotsAsync(date, cancellationToken);

        var slots = new List<AvailableSlotDto>();
        var cursor = open;
        while (cursor.AddMinutes(DefaultDurationMinutes) <= close.AddMinutes(1))
        {
            var slotStart = date.ToDateTime(cursor);
            var slotEnd = slotStart.AddMinutes(DefaultDurationMinutes);

            var isBooked = booked.Any(b =>
            {
                var bStart = b.ScheduledAt.DateTime;
                var bEnd = bStart.AddMinutes(b.DurationMinutes <= 0 ? DefaultDurationMinutes : b.DurationMinutes);
                return bStart < slotEnd && bEnd > slotStart;
            });

            slots.Add(new AvailableSlotDto(
                date.ToString("yyyy-MM-dd"),
                cursor.ToString("HH:mm"),
                !isBooked));

            cursor = cursor.AddMinutes(DefaultDurationMinutes);
        }

        return slots;
    }

    private sealed class DayHours
    {
        [JsonPropertyName("open")] public string? Open { get; set; }
        [JsonPropertyName("close")] public string? Close { get; set; }
        [JsonPropertyName("closed")] public bool Closed { get; set; }
    }
}
