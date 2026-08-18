import { useCallback, useEffect, useState } from "react";
import { listGateways, type Gateway } from "@/api/endpoints/gateways";
import { ApiError } from "@/api/client";

export function useGateways() {
  const [gateways, setGateways] = useState<Gateway[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const load = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      setGateways(await listGateways());
    } catch (err) {
      setError(
        err instanceof ApiError ? err.message : "An unexpected error occurred.",
      );
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    load();
  }, [load]);
  return { gateways, loading, error, reload: load };
}
