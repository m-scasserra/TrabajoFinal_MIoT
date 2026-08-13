import { useEffect, useState } from "react";
import {
  getDeviceProfile,
  type DeviceProfile,
} from "@/api/endpoints/deviceProfiles";
import { ApiError } from "@/api/client";

interface UseDeviceProfileResult {
  profile: DeviceProfile | null;
  loading: boolean;
  error: string | null;
}
export function useDeviceProfile(
  id: string | undefined,
): UseDeviceProfileResult {
  const [profile, setProfile] = useState<DeviceProfile | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!id) {
      setError("No device profile ID provided.");
      setLoading(false);
      return;
    }
    let cancelled = false;
    (async () => {
      setLoading(true);
      setError(null);
      try {
        const p = await getDeviceProfile(id);
        if (!cancelled) setProfile(p);
      } catch (err) {
        if (cancelled) return;
        if (err instanceof ApiError && err.status === 404)
          setError("Device profile not found.");
        else setError("Could not fetch device profile.");
      } finally {
        if (!cancelled) setLoading(false);
      }
    })();
    return () => {
      cancelled = true;
    };
  }, [id]);

  return { profile, loading, error };
}
