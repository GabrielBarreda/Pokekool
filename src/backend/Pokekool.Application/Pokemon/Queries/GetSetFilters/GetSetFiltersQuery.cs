using MediatR;
using Pokekool.Shared.Dtos.Pokemon;

namespace Pokekool.Application.Pokemon.Queries.GetSetFilters;

public sealed record GetSetFiltersQuery(Guid SetId) : IRequest<SetFiltersDto>;
