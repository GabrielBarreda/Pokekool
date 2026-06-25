using MediatR;
using Pokekool.Shared.Dtos.Dashboard;

namespace Pokekool.Application.Dashboard.Queries.GetDashboardSummary;

public sealed record GetDashboardSummaryQuery : IRequest<DashboardSummaryDto>;
