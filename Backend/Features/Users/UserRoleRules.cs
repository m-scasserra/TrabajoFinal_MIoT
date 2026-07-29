using Backend.Common.Security;

namespace Backend.Features.Users;

public static class UserRoleRules
{
    public static bool CanAssign(string creatorRole, string targetRole) => creatorRole switch
    {
        Roles.SuperAdmin => targetRole is Roles.OrgAdmin or Roles.Operator,
        Roles.OrgAdmin => targetRole is Roles.OrgAdmin or Roles.Operator,
        _ => false
    };
}