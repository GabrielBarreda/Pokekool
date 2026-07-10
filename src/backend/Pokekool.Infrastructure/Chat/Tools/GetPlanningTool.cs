using Pokekool.Application.Chat;
using Pokekool.Domain.Entities.Shop;
using Pokekool.Domain.Repositories;

namespace Pokekool.Infrastructure.Chat.Tools;

public sealed class GetPlanningTool : IChatTool
{
    private readonly IShopAppointmentRepository _repo;

    public string Name => "get_planning";
    public string Description => "Retourne les RDV du planning sur une période donnée.";
    public object ParametersSchema => new
    {
        type = "object",
        properties = new
        {
            from = new { type = "string", description = "Date début ISO 8601 (ex: '2026-07-07T00:00:00Z')" },
            to = new { type = "string", description = "Date fin ISO 8601" },
            status = new { type = "string", description = "Filtrer par statut: Pending, Confirmed, InProgress, Completed, Cancelled" }
        },
        required = new[] { "from", "to" }
    };

    public GetPlanningTool(IShopAppointmentRepository repo) => _repo = repo;

    public async Task<string> ExecuteAsync(IDictionary<string, object?> args, ChatSession session, CancellationToken ct)
    {
        var from = DateTimeOffset.TryParse(args.GetValueOrDefault("from")?.ToString(), out var f) ? f : DateTimeOffset.UtcNow;
        var to = DateTimeOffset.TryParse(args.GetValueOrDefault("to")?.ToString(), out var t) ? t : from.AddDays(7);

        AppointmentStatus? status = Enum.TryParse<AppointmentStatus>(
            args.GetValueOrDefault("status")?.ToString(), ignoreCase: true, out var st) ? st : null;

        var (items, total) = await _repo.SearchAsync(status, serviceType: null, from, to, page: 1, pageSize: 50, ct);

        if (items.Count == 0)
            return "Aucun RDV sur cette période.";

        var lines = items.Select(a =>
            $"- [{a.Id}] {a.ScheduledAt:dd/MM HH:mm} · {a.ServiceType} · {a.Status} · {a.ClientName}");
        return $"Planning ({total} RDV) :\n" + string.Join("\n", lines);
    }
}
