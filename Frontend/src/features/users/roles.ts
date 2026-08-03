export const Roles = {
  SuperAdmin: "SUPERADMIN",
  OrgAdmin: "ORG_ADMIN",
  Operator: "OPERATOR",
} as const;

export type Role = (typeof Roles)[keyof typeof Roles];

export function assignableRoles(creatorRole: string): Role[] {
  switch (creatorRole) {
    case Roles.SuperAdmin:
    case Roles.OrgAdmin:
      return [Roles.OrgAdmin, Roles.Operator];
    case Roles.Operator:
      return [];
    default:
      return [];
  }
}

export const roleLabels: Record<Role, string> = {
  [Roles.SuperAdmin]: "Super Admin",
  [Roles.OrgAdmin]: "Organisation Admin",
  [Roles.Operator]: "Operator",
};
