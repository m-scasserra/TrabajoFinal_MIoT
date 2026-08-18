import { useCallback, useEffect, useState } from "react";
import { listNodes, type Node } from "@/api/endpoints/nodes";
import { ApiError } from "@/api/client";

export function useNodes() {
  const [nodes, setNodes] = useState<Node[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const load = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      setNodes(await listNodes());
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
  return { nodes, loading, error, reload: load };
}
