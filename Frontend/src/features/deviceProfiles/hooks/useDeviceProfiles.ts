import { useCallback, useEffect, useState } from "react";
import {
  listDeviceProfiles,
  type DeviceProfile,
} from "@/api//endpoints/deviceProfiles";
import { ApiError } from "@/api/client";

interface UseDeviceProfilesResult {
  profiles: DeviceProfile[];
  loading: boolean;
  error: string | null;
  reload: () => void;
}

export function useDeviceProfiles(): UseDeviceProfilesResult {
  const [profiles, setProfiles] = useState<DeviceProfile[]>([]);
  const [loading, setLoading] = useState<boolean>(true);
  const [error, setError] = useState<string | null>(null);

  const load = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      setProfiles(await listDeviceProfiles());
    } catch (err) {
      if (err instanceof ApiError && err.status === 403) {
        setError("You do not have permission to view device profiles.");
      } else {
        setError("An error occurred while loading device profiles.");
      }
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    load();
  }, [load]);

  return { profiles, loading, error, reload: load };
}
