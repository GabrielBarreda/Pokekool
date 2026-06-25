using MediatR;
using Pokekool.Domain.Repositories;
using Pokekool.Shared.Dtos.Common;
using Pokekool.Shared.Dtos.Users;

namespace Pokekool.Application.Users.Queries;

public sealed record GetUsersQuery(
    string? Search = null,
    string? RoleCode = null,
    bool? HasPendingMessages = null,   // filtre futur — table Message non encore créée
    bool? RecentOnly = null,
    string? SortBy = null,
    int Page = 1,
    int PageSize = 30
) : IRequest<PagedResult<UserSummaryDto>>;

public sealed class GetUsersQueryHandler : IRequestHandler<GetUsersQuery, PagedResult<UserSummaryDto>>
{
    private readonly IUserRepository _users;
    private readonly IRoleRepository _roles;

    public GetUsersQueryHandler(IUserRepository users, IRoleRepository roles)
    {
        _users = users;
        _roles = roles;
    }

    public async Task<PagedResult<UserSummaryDto>> Handle(GetUsersQuery request, CancellationToken cancellationToken)
    {
        // Résoudre le code de rôle en ID si fourni
        Guid? roleId = null;
        if (!string.IsNullOrWhiteSpace(request.RoleCode))
        {
            var role = await _roles.GetByCodeAsync(tenantId: null, request.RoleCode, cancellationToken);
            roleId = role?.Id;

            // Filtre incompatible → aucun résultat
            if (roleId is null)
                return new PagedResult<UserSummaryDto>([], request.Page, request.PageSize, 0, 0);
        }

        var (items, total) = await _users.GetPagedAsync(
            search:     request.Search,
            roleId:     roleId,
            recentOnly: request.RecentOnly,
            sortBy:     request.SortBy,
            page:       Math.Max(1, request.Page),
            pageSize:   Math.Clamp(request.PageSize, 1, 100),
            ct:         cancellationToken);

        // Charger les rôles pour le mapping
        var roleIds = items.SelectMany(u => u.RoleAssignments).Select(ra => ra.RoleId).Distinct().ToList();
        var rolesById = (await _roles.GetManyByIdsAsync(roleIds, cancellationToken)).ToDictionary(r => r.Id);

        var dtos = items.Select(u => u.ToSummaryDto(rolesById)).ToList();

        var totalPages = total == 0 ? 0 : (int)Math.Ceiling((double)total / request.PageSize);

        return new PagedResult<UserSummaryDto>(dtos, request.Page, request.PageSize, total, totalPages);
    }
}
