namespace Backend.Common.Security;

public static class Roles
{
    public const string SuperAdmin = "SUPERADMIN";
    public const string OrgAdmin = "ORG_ADMIN";
    public const string Operator = "OPERATOR";
}

public static class Policies
{
    public const string SuperAdminOnly = "SuperAdminOnly";
    public const string OrgAdminOrAbove = "OrgAdminOrAbove";
}