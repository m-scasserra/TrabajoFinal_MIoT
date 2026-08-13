import { useState } from "react";
import {
  createDeviceProfile,
  updateDeviceProfile,
  type DeviceProfilePayload,
  type DeviceProfile,
} from "@/api/endpoints/deviceProfiles";
import { ApiError } from "@/api/client";

interface UseSaveResult {
  save: (
    payload: DeviceProfilePayload,
    id?: string,
  ) => Promise<DeviceProfile | null>;
  saving: boolean;
  error: string | null;
}

export function useSaveDeviceProfile(): UseSaveResult {
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const save = async (payload: DeviceProfilePayload, id?: string) => {
    setSaving(true);
    setError(null);
    try {
      return id
        ? await updateDeviceProfile(id, payload)
        : await createDeviceProfile(payload);
    } catch (err) {
      if (err instanceof ApiError) setError(err.message);
      else setError("An unexpected error occurred.");
      return null;
    } finally {
      setSaving(false);
    }
  };

  return { save, saving, error };
}
