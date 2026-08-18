import { apiFetch } from "../client";
import type { SyncStatus, OperativeState } from "../types";

export interface Gateway {
  gatewayEui: string;
  orgId: string | null;
  alias: string;
  model: string;
  operativeState: OperativeState;
  syncStatus: SyncStatus;
  syncError: string | null;
  latitude: number | null;
  longitude: number | null;
  lastSeen: string | null;
  createdAt: string;
}

export interface CreateGatewayPayload {
  gatewayEui: string;
  alias: string;
  model: string;
  latitude?: number | null;
  longitude?: number | null;
}

export interface UpdateGatewayPayload {
  alias: string;
  model: string;
  operativeState: OperativeState;
  latitude?: number | null;
  longitude?: number | null;
}

export function listGateways(): Promise<Gateway[]> {
  return apiFetch<Gateway[]>("/gateways", { method: "GET" });
}

export function getGateway(eui: string): Promise<Gateway> {
  return apiFetch<Gateway>(`/gateways/${eui}`, { method: "GET" });
}

export function createGateway(payload: CreateGatewayPayload): Promise<Gateway> {
  return apiFetch<Gateway>("/gateways", {
    method: "POST",
    body: JSON.stringify(payload),
  });
}

export function updateGateway(
  eui: string,
  payload: UpdateGatewayPayload,
): Promise<Gateway> {
  return apiFetch<Gateway>(`/gateways/${eui}`, {
    method: "PUT",
    body: JSON.stringify(payload),
  });
}

export function deleteGateway(
  eui: string,
): Promise<{ message: string; eui: string }> {
  return apiFetch<{ message: string; eui: string }>(`/gateways/${eui}`, {
    method: "DELETE",
  });
}
