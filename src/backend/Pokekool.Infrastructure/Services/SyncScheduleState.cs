namespace Pokekool.Infrastructure.Services;

public enum SyncScheduleStatus { None, Pending, Running, Completed, Cancelled }

/// <summary>
/// État en mémoire du sync planifié (singleton, non persisté).
/// Remis à None au redémarrage du backend.
/// </summary>
public sealed class SyncScheduleState
{
    public TimeOnly?           ScheduledTime      { get; set; }
    public bool                IsRecurring        { get; set; }
    public DateTime?           NextRunAt          { get; set; }   // UTC
    public SyncScheduleStatus  Status             { get; set; } = SyncScheduleStatus.None;
    public DateTime?           LastCompletedAt    { get; set; }
    public int?                LastPricesUpserted { get; set; }
}
