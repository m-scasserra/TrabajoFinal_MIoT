import { useState } from "react";
import {
  provisionNode,
  type ProvisionNodePayload,
  type ProvisionNodeResponse,
} from "@/api/endpoints/nodes";
import { ApiError } from "@/api/client";

export function useProvisionNode() {
  const [provisioning, setProvisioning] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const provision = async (
    payload: ProvisionNodePayload,
  ): Promise<ProvisionNodeResponse | null> => {
    setProvisioning(true);
    setError(null);
    try {
      return await provisionNode(payload);
    } catch (err) {
      setError(
        err instanceof ApiError ? err.message : "An unexpected error occurred.",
      );
      return null;
    } finally {
      setProvisioning(false);
    }
  };

  return { provision, provisioning, error };
}
