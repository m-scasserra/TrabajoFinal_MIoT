import { Roles } from "../users/roles";
import type { Gateway } from "@/api/endpoints/gateways";

export function isSystemGateway(gateway: Pick<Gateway, "orgId">): boolean {
  return gateway.orgId === null;
}

export function canModifyGateway(
  userRole: string | undefined,
  gateway: Pick<Gateway, "orgId">,
): boolean {
  if (userRole === Roles.SuperAdmin) return true;
  return !isSystemGateway(gateway);
}
