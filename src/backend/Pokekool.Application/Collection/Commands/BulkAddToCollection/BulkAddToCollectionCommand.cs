using MediatR;
using Pokekool.Shared.Dtos.Collection;

namespace Pokekool.Application.Collection.Commands.BulkAddToCollection;

public sealed record BulkAddToCollectionCommand(
    IReadOnlyList<Guid> CardIds,
    IReadOnlyList<CardScanMatchDto> Matches // pour mise à jour du Vault
) : IRequest<BulkAddResult>;
