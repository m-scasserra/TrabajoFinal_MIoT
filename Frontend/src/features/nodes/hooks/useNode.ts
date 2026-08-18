import { useEffect, useState } from "react";
import { getNode, type Node } from "@/api/endpoints/nodes";
import { ApiError } from "@/api/client";

export function useNode(devEui: string | undefined) {
  const [node, setNode] = useState<Node | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!devEui) {
      setError("Device EUI is required");
      setLoading(false);
      return;
    }
    let cancelled = false;
    (async () => {
      setLoading(true);
      setError(null);
      try {
        const n = await getNode(devEui);
        if (!cancelled) setNode(n);
      } catch (err) {
        if (cancelled) return;
        if (err instanceof ApiError && err.status === 404)
          setError("Node not found");
        else setError("An unexpected error occurred.");
      } finally {
        if (!cancelled) setLoading(false);
      }
    })();
    return () => {
      cancelled = true;
    };
  }, [devEui]);

  return { node, loading, error };
}
