import { useCallback, useEffect, useState } from "react";
import { listUsers, type UserListItem } from "../../../api/endpoints/users";
import { ApiError } from "../../../api/client";

interface UseUsersResult {
  users: UserListItem[];
  loading: boolean;
  error: string | null;
  reload: () => void;
}

export function useUsers(): UseUsersResult {
  const [users, setUsers] = useState<UserListItem[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const load = useCallback(async () => {
    setLoading(true);
    setError(null);

    try {
      setUsers(await listUsers());
    } catch (err) {
      if (err instanceof ApiError && err.status === 403) {
        setError("You do not have permission to view users.");
      } else {
        setError("An error occurred while loading users.");
      }
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    load();
  }, [load]);

  return { users, loading, error, reload: load };
}
