import { apiFetch } from "../client";

export interface Organisation {
  id: string;
  name: string;
  cuit: string | null;
  active: boolean;
  createdAt: string;
}

export interface CreateOrganisationPayload {
  name: string;
  cuit?: string | null;
  adminFullname: string;
  adminEmail: string;
}

export function createOrganisation(
  payload: CreateOrganisationPayload,
): Promise<Organisation> {
  return apiFetch<Organisation>("/organisations", {
    method: "POST",
    body: JSON.stringify(payload),
  });
}
