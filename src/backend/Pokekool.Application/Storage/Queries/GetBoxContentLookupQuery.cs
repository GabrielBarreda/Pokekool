using MediatR;
using Pokekool.Domain.Common;
using Pokekool.Domain.Repositories.Storage;
using Pokekool.Shared.Dtos.Storage;

namespace Pokekool.Application.Storage.Queries;

/// <summary>
/// Lookup "scan théorique" : recherche un carton par code-barre OU code interne,
/// et retourne son contenu attendu (classeurs + localisations). Pas de lecture caméra
/// dans ce prompt — juste une recherche texte (l'intégration caméra viendra avec l'app mobile).
/// </summary>
public sealed record GetBoxContentLookupQuery(string Code) : IRequest<StorageBoxLookupDto>;

internal sealed class GetBoxContentLookupQueryHandler
    : IRequestHandler<GetBoxContentLookupQuery, StorageBoxLookupDto>
{
    private readonly IStorageBoxRepository _boxRepo;
    private readonly IStorageBinderRepository _binderRepo;
    private readonly IStorageLocationRepository _locationRepo;

    public GetBoxContentLookupQueryHandler(
        IStorageBoxRepository boxRepo,
        IStorageBinderRepository binderRepo,
        IStorageLocationRepository locationRepo)
    {
        _boxRepo = boxRepo;
        _binderRepo = binderRepo;
        _locationRepo = locationRepo;
    }

    public async Task<StorageBoxLookupDto> Handle(GetBoxContentLookupQuery request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Code))
            throw new DomainException("storage.lookup.empty_code", "Code de recherche vide.");

        var box = await _boxRepo.FindByCodeAsync(request.Code.Trim(), ct)
            ?? throw new DomainException("storage.box.not_found", "Aucun carton pour ce code.");

        var binders = await _binderRepo.ListByBoxAsync(box.Id, ct);
        var binderIds = binders.Select(b => b.Id).ToList();
        var locations = await _locationRepo.ListByBinderIdsAsync(binderIds, ct);
        var locationsByBinder = locations.GroupBy(l => l.BinderId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var binderDtos = binders.Select(binder =>
        {
            var locs = locationsByBinder.TryGetValue(binder.Id, out var list)
                ? list.Select(StorageMapper.ToDto).ToList()
                : new List<StorageLocationDto>();
            return new StorageBinderWithLocationsDto(StorageMapper.ToDto(binder), locs);
        }).ToList();

        return new StorageBoxLookupDto(StorageMapper.ToDto(box), binderDtos);
    }
}
