import { useState } from "react";
import { updateUser, type UpdateUserPayload } from "@/api/endpoints/users";
import { ApiError } from "@/api/client";

interface UseUpdateUserResult {
  submit: (id: string, payload: UpdateUserPayload) => Promise<boolean>;
  submitting: boolean;
  error: string | null;
}

export function useUpdateUser(): UseUpdateUserResult {
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const submit = async (
    id: string,
    payload: UpdateUserPayload,
  ): Promise<boolean> => {
    setSubmitting(true);
    setError(null);
    try {
      await updateUser(id, payload);
      return true;
    } catch (err) {
      if (err instanceof ApiError && err.status === 404) {
        setError("User not found.");
      } else if (err instanceof ApiError) {
        setError(err.message);
      } else {
        setError("Could not update the user. Please try again.");
      }
      return false;
    } finally {
      setSubmitting(false);
    }
  };

  return { submit, submitting, error };
}
