using MediatR;
using Pokekool.Shared.Dtos.Collection;

namespace Pokekool.Application.Collection.Commands.ScanCollection;

public sealed record ScanCollectionCommand(string ImageBase64) : IRequest<ScanCollectionResult>;
