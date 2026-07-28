namespace Backend.Features.Auth.Models;

internal record SessionQueryResult(Guid SessionId, Guid UserId, string Role, Guid? OrgId, bool Active);