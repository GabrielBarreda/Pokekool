using System;
using System.Collections.Generic;

namespace Pokekool.Shared.Dtos.Auth;

public sealed record CurrentUserDto(
    Guid? Id,
    string? Email,
    Guid? TenantId,
    IReadOnlyList<string> Roles,
    bool IsAuthenticated,
    string FirstName,
    string LastName,
    IReadOnlyCollection<string> Permissions);
