import { apiFetch } from "../client";

export type SyncStatus =
  | "PENDING"
  | "SYNCED"
  | "FAILED"
  | "PENDING_DELETE"
  | "DELETE_FAILED";

export interface AppLayerParams {
  ts003FPort?: number;
  ts004FPort?: number;
  ts005FPort?: number;
}

export interface DeviceProfile {
  id: string;
  chirpstackId: string | null;
  name: string;
  region: string;
  macVersion: string;
  regParamsRevision: string;
  regionConfigId: string | null;
  adrAlgorithmId: string;
  uplinkInterval: number;
  deviceStatusReqInterval: number;
  supportsOtaa: boolean;
  flushQueueOnActivate: boolean;
  autoDetectMeasurements: boolean;
  appLayerParams: AppLayerParams | null;
  syncStatus: SyncStatus;
  syncError: string | null;
  createdAt: string;
}

export interface DeviceProfilePayload {
  name: string;
  region: string;
  macVersion: string;
  regParamsRevision: string;
  regionConfigId?: string | null;
  adrAlgorithmId?: string;
  uplinkInterval: number;
  deviceStatusReqInterval: number;
  supportsOtaa: boolean;
  flushQueueOnActivate: boolean;
  autoDetectMeasurements: boolean;
  appLayerParams?: AppLayerParams | null;
}

export function listDeviceProfiles(): Promise<DeviceProfile[]> {
  return apiFetch<DeviceProfile[]>("/device-profiles", { method: "GET" });
}

export function getDeviceProfile(id: string): Promise<DeviceProfile> {
  return apiFetch<DeviceProfile>(`/device-profiles/${id}`, { method: "GET" });
}

export function createDeviceProfile(
  payload: DeviceProfilePayload,
): Promise<DeviceProfile> {
  return apiFetch<DeviceProfile>("/device-profiles", {
    method: "POST",
    body: JSON.stringify(payload),
  });
}

export function updateDeviceProfile(
  id: string,
  payload: DeviceProfilePayload,
): Promise<DeviceProfile> {
  return apiFetch<DeviceProfile>(`/device-profiles/${id}`, {
    method: "PUT",
    body: JSON.stringify(payload),
  });
}

export function deleteDeviceProfile(id: string): Promise<{ message: string }> {
  return apiFetch<{ message: string }>(`/device-profiles/${id}`, {
    method: "DELETE",
  });
}
