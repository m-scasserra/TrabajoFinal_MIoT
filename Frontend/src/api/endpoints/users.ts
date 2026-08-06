import { apiFetch } from "../client";

export interface UserListItem {
  id: string;
  orgId: string | null;
  fullname: string;
  email: string;
  role: string;
  active: boolean;
  emailVerified: boolean;
  createdAt: string;
}

export interface CreateUserPayload {
  fullname: string;
  email: string;
  role: string;
}

export interface UpdateUserPayload {
  fullname: string;
  active: boolean;
}

export function listUsers(): Promise<UserListItem[]> {
  return apiFetch<UserListItem[]>("/users", { method: "GET" });
}

export function getUser(id: string): Promise<UserListItem> {
  return apiFetch<UserListItem>(`/users/${id}`, { method: "GET" });
}

export function createUser(
  payload: CreateUserPayload,
): Promise<{ id: string }> {
  return apiFetch<{ id: string }>("/users", {
    method: "POST",
    body: JSON.stringify(payload),
  });
}

export function updateUser(
  id: string,
  payload: UpdateUserPayload,
): Promise<void> {
  return apiFetch<void>(`/users/${id}`, {
    method: "PUT",
    body: JSON.stringify(payload),
  });
}
