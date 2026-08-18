import { useState } from "react";
import {
  createGateway,
  updateGateway,
  type CreateGatewayPayload,
  type UpdateGatewayPayload,
  type Gateway,
} from "@/api/endpoints/gateways";
import { ApiError } from "@/api/client";

export function useSaveGateway() {
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const create = async (
    payload: CreateGatewayPayload,
  ): Promise<Gateway | null> => {
    setSaving(true);
    try {
      return await createGateway(payload);
    } catch (err) {
      setError(
        err instanceof ApiError ? err.message : "An unexpected error occurred.",
      );
      return null;
    } finally {
      setSaving(false);
    }
  };

  const update = async (
    eui: string,
    payload: UpdateGatewayPayload,
  ): Promise<Gateway | null> => {
    setSaving(true);
    try {
      return await updateGateway(eui, payload);
    } catch (err) {
      setError(
        err instanceof ApiError ? err.message : "An unexpected error occurred.",
      );
      return null;
    } finally {
      setSaving(false);
    }
  };

  return { create, update, saving, error };
}
