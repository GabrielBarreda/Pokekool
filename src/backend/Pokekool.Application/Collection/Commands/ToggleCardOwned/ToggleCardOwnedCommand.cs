using MediatR;
using Pokekool.Shared.Dtos.Collection;

namespace Pokekool.Application.Collection.Commands.ToggleCardOwned;

public sealed record ToggleCardOwnedCommand(Guid CardId) : IRequest<CollectionCardStatusDto>;
