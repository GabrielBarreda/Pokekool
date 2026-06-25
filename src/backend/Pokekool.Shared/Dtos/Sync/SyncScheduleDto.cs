namespace Pokekool.Shared.Dtos.Sync;

public sealed record SyncScheduleDto(
    string?  ScheduledTime,       // "HH:mm" heure locale
    bool     IsRecurring,
    DateTime? NextRunAt,          // UTC
    string   Status,              // "none" | "pending" | "running" | "completed" | "cancelled"
    DateTime? LastCompletedAt,
    int?     LastPricesUpserted);
