using System.Security.Claims;

namespace Backend.Common.Security;

public sealed class CurrentUser(IHttpContextAccessor accessor)
{
    private ClaimsPrincipal? User => accessor.HttpContext?.User;

    public bool isAuthenticated => User?.Identity?.IsAuthenticated ?? false;

    public Guid UserId =>
        Guid.TryParse(User?.FindFirstValue(ClaimTypes.NameIdentifier), out var id)
            ? id
            : throw new UnauthorizedAccessException("User claim is missing or invalid.");

    public string Role =>
        User?.FindFirstValue(ClaimTypes.Role)
        ?? throw new UnauthorizedAccessException("User role claim is missing.");

    public bool IsSuperAdmin => Role == Roles.SuperAdmin;

    public Guid? OrgId =>
        Guid.TryParse(User?.FindFirstValue("org_id"), out var id) ? id : null;

    public Guid RequireOrgId() =>
        OrgId ?? throw new UnauthorizedAccessException("User organization claim is missing or invalid.");
}