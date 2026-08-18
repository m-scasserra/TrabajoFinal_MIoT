import { useState } from "react";
import {
  activateNode,
  type ActivateNodePayload,
  type Node,
} from "@/api/endpoints/nodes";
import { ApiError } from "@/api/client";

export function useActivateNode() {
  const [activating, setActivating] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const activate = async (
    payload: ActivateNodePayload,
  ): Promise<Node | null> => {
    setActivating(true);
    setError(null);
    try {
      return await activateNode(payload);
    } catch (err) {
      setError(
        err instanceof ApiError ? err.message : "An unexpected error occurred.",
      );
      return null;
    } finally {
      setActivating(false);
    }
  };

  return { activate, activating, error };
}
