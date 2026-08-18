import { useCallback, useEffect, useState } from "react";
import { listProvisionedNodes, type Node } from "@/api/endpoints/nodes";
import { ApiError } from "@/api/client";

export function useProvisionedNodes() {
  const [nodes, setNodes] = useState<Node[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const load = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      setNodes(await listProvisionedNodes());
    } catch (err) {
      if (err instanceof ApiError && err.status === 403) {
        setError("You do not have permission to view provisioned devices.");
      } else {
        setError(
          "An unexpected error occurred while loading provisioned devices.",
        );
      }
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    load();
  }, [load]);
  return { nodes, loading, error, reload: load };
}
