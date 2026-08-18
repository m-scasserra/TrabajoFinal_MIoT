import { apiFetch } from "../client";
import type { SyncStatus, OperativeState } from "../types";

export type MeterType = "MODBUS_RTU" | "PULSE" | "ANALOG";

export interface Node {
  id: string;
  devEui: string;
  macAddress: string;
  orgId: string | null;
  alias: string;
  meterType: MeterType | null;
  operativeState: OperativeState;
  syncStatus: SyncStatus;
  syncError: string | null;
  hwRevision: number;
  fwRevision: number;
  latitude: number | null;
  longitude: number | null;
  createdAt: string;
}

export interface ActivateNodePayload {
  macAddress: string;
  deviceProfileId: string;
  alias: string;
  meterType: MeterType;
  freqMinutes?: number | null;
  latitude?: number | null;
  longitude?: number | null;
}

export interface ProvisionNodePayload {
  macAddress: string;
  hwRevision: number;
  fwRevision: number;
}

export interface ProvisionNodeResponse {
  id: string;
  devEui: string;
  macAddress: string;
  hwRevision: number;
  fwRevision: number;
  appKey: string;
}

export interface UpdateProvisioningPayload {
  hwRevision: number;
  fwRevision: number;
}

export function listNodes(): Promise<Node[]> {
  return apiFetch<Node[]>("/nodes", { method: "GET" });
}

export function getNode(devEui: string): Promise<Node> {
  return apiFetch<Node>(`/nodes/${devEui}`, { method: "GET" });
}

export function activateNode(payload: ActivateNodePayload): Promise<Node> {
  return apiFetch<Node>("/nodes/activate", {
    method: "POST",
    body: JSON.stringify(payload),
  });
}

export function releaseNode(id: string): Promise<{ message: string }> {
  return apiFetch<{ message: string }>(`/nodes/${id}`, { method: "DELETE" });
}

export function provisionNode(
  payload: ProvisionNodePayload,
): Promise<ProvisionNodeResponse> {
  return apiFetch<ProvisionNodeResponse>("/provisioning/nodes", {
    method: "POST",
    body: JSON.stringify(payload),
  });
}

export function updateProvisioning(
  id: string,
  payload: UpdateProvisioningPayload,
): Promise<Node> {
  return apiFetch<Node>(`/provisioning/nodes/${id}`, {
    method: "PUT",
    body: JSON.stringify(payload),
  });
}

export function rotateNodeKey(id: string): Promise<{ appKey: string }> {
  return apiFetch<{ appKey: string }>(`/provisioning/nodes/${id}/rotate-key`, {
    method: "POST",
  });
}

export function listProvisionedNodes(): Promise<Node[]> {
  return apiFetch<Node[]>("/provisioning/nodes", { method: "GET" });
}
