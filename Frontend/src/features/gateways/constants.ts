import type { OperativeState } from "@/api/types";

export const OPERATIVE_STATES: OperativeState[] = [
  "ACTIVE",
  "INACTIVE",
  "MAINTENANCE",
];

export const operativeStateLabels: Record<OperativeState, string> = {
  ACTIVE: "Active",
  INACTIVE: "Inactive",
  MAINTENANCE: "Maintenance",
};

const ONLINE_THRESHOLD_MS = 5 * 60 * 1000;

export function isGatewayOnline(lastSeen: string | null): boolean {
  if (!lastSeen) return false;
  return Date.now() - new Date(lastSeen).getTime() < ONLINE_THRESHOLD_MS;
}
