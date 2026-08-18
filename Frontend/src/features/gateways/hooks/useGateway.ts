import { useEffect, useState } from "react";
import { getGateway, type Gateway } from "@/api/endpoints/gateways";
import { ApiError } from "@/api/client";

export function useGateway(eui: string | undefined) {
  const [gateway, setGateway] = useState<Gateway | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!eui) {
      setError("Gateway EUI is undefined");
      setLoading(false);
      return;
    }
    let cancelled = false;
    (async () => {
      setLoading(true);
      setError(null);
      try {
        const g = await getGateway(eui);
        if (!cancelled) setGateway(g);
      } catch (err) {
        if (cancelled) return;
        if (err instanceof ApiError && err.status === 404)
          setError("Gateway not found");
        else setError("An unexpected error occurred.");
      } finally {
        if (!cancelled) setLoading(false);
      }
    })();
    return () => {
      cancelled = true;
    };
  }, [eui]);

  return { gateway, loading, error };
}
