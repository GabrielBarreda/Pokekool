namespace Pokekool.Shared.Dtos.Users;

/// <summary>Remplace l'ensemble des rôles d'un user par ceux fournis (idempotent).</summary>
public sealed record AssignRolesRequest(IReadOnlyList<Guid> RoleIds);
