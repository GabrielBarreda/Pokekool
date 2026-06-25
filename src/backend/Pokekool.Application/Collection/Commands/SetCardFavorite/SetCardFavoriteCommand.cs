using MediatR;
using Pokekool.Shared.Dtos.Collection;

namespace Pokekool.Application.Collection.Commands.SetCardFavorite;

public sealed record SetCardFavoriteCommand(Guid CardId, bool IsFavorite) : IRequest<CollectionCardStatusDto>;
