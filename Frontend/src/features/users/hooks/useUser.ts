import { useCallback, useEffect, useState } from "react";
import { getUser, type UserListItem } from "@/api/endpoints/users";
import { ApiError } from "@/api/client";

interface UseUserResult {
  user: UserListItem | null;
  loading: boolean;
  error: string | null;
}

export function useUser(id: string | undefined): UseUserResult {
  const [user, setUser] = useState<UserListItem | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const load = useCallback(async () => {
    if (!id) {
      setError("User ID is required.");
      return;
    }
    setLoading(true);
    setError(null);
    try {
      setUser(await getUser(id));
    } catch (err) {
      if (err instanceof ApiError && err.status === 404) {
        setError("User not found.");
      } else if (err instanceof ApiError && err.status === 403) {
        setError("You do not have permission to view this user.");
      } else {
        setError("An unexpected error occurred. Please try again.");
      }
    } finally {
      setLoading(false);
    }
  }, [id]);

  useEffect(() => {
    load();
  }, [load]);

  return { user, loading, error };
}
