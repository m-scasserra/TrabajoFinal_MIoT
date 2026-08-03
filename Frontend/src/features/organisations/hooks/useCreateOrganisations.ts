import { useState } from "react";
import {
  createOrganisation,
  type CreateOrganisationPayload,
  type Organisation,
} from "../../../api/endpoints/organisations";
import { ApiError } from "../../../api/client";

interface UseCreateOrganisationResult {
  submit: (payload: CreateOrganisationPayload) => Promise<Organisation | null>;
  submitting: boolean;
  error: string | null;
}

export function useCreateOrganisation(): UseCreateOrganisationResult {
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const submit = async (
    payload: CreateOrganisationPayload,
  ): Promise<Organisation | null> => {
    setSubmitting(true);
    setError(null);
    try {
      return await createOrganisation(payload);
    } catch (err) {
      if (err instanceof ApiError) {
        setError(
          err.status === 403
            ? "You do not have permission to create organisations."
            : err.message,
        );
      } else {
        setError("An error occurred while creating the organisation.");
      }
      return null;
    } finally {
      setSubmitting(false);
    }
  };

  return { submit, submitting, error };
}
