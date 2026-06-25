using MediatR;
using Pokekool.Shared.Dtos.Collection;

namespace Pokekool.Application.Collection.Commands.SetCardPriority;

public sealed record SetCardPriorityCommand(Guid CardId, bool IsPriority) : IRequest<CollectionCardStatusDto>;
