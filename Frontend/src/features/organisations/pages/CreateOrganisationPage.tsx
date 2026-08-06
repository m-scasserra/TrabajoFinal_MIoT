import { useState, type FormEvent } from "react";
import { useNavigate } from "react-router-dom";
import { useAuth } from "../../auth/AuthContext";
import { Roles } from "../../users/roles";
import { useCreateOrganisation } from "../hooks/useCreateOrganisations";
import { useSetTopBar } from "@/components/layout/TopBarContext";
import type { Organisation } from "@/api/endpoints/organisations";
import { FormField } from "@/components/FormField";
import { Button } from "@/components/ui/button";
import { Separator } from "@/components/ui/separator";
import {
  Card,
  CardContent,
  CardHeader,
  CardTitle,
  CardDescription,
} from "@/components/ui/card";

export function CreateOrganisationPage() {
  const { user } = useAuth();
  const navigate = useNavigate();
  const { submit, submitting, error } = useCreateOrganisation();

  useSetTopBar("Create Organisation");

  const [name, setName] = useState("");
  const [cuit, setCuit] = useState("");
  const [adminFullname, setAdminFullname] = useState("");
  const [adminEmail, setAdminEmail] = useState("");
  const [created, setCreated] = useState<Organisation | null>(null);

  if (user?.role !== Roles.SuperAdmin) {
    return (
      <p className="text-sm text-destructive bg-destructive/10 border border-destructive/20 rounded-md px-4 py-3">
        You do not have permission to create organisations.
      </p>
    );
  }

  if (created) {
    return (
      <Card className="max-w-md border-green-200 bg-green-50/50">
        <CardHeader>
          <CardTitle>Organisation Created</CardTitle>
          <CardDescription>
            <span className="font-medium text-foreground">{created.name}</span>{" "}
            has been created. An invitation has been sent to{" "}
            <span className="font-medium text-foreground">{adminEmail}</span>{" "}
            for the administrator to set up their password.
          </CardDescription>
        </CardHeader>
        <CardContent className="flex gap-3">
          <Button
            onClick={() => {
              setCreated(null);
              setName("");
              setCuit("");
              setAdminFullname("");
              setAdminEmail("");
            }}
          >
            Create Another
          </Button>
          <Button variant="outline" onClick={() => navigate("/")}>
            Go to Home
          </Button>
        </CardContent>
      </Card>
    );
  }

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault();
    const result = await submit({
      name,
      cuit: cuit.trim() === "" ? null : cuit.trim(),
      adminFullname,
      adminEmail,
    });
    if (result) setCreated(result);
  };

  return (
    <div className="max-w-md">
      <p className="text-sm text-muted-foreground mb-5">
        The organisation will be created along with its administrator, who will
        receive an invitation by email.
      </p>

      <form onSubmit={handleSubmit} className="space-y-4" noValidate>
        <FormField
          id="name"
          label="Organisation Name"
          type="text"
          required
          maxLength={100}
          value={name}
          onChange={(e) => setName(e.target.value)}
          disabled={submitting}
        />
        <FormField
          id="cuit"
          label="CUIT"
          type="text"
          maxLength={20}
          value={cuit}
          onChange={(e) => setCuit(e.target.value)}
          disabled={submitting}
        />

        <div className="pt-2">
          <Separator />
          <p className="text-sm font-medium text-foreground mt-3">
            Administrador
          </p>
        </div>

        <FormField
          id="adminFullName"
          label="Full Name"
          type="text"
          required
          maxLength={150}
          value={adminFullname}
          onChange={(e) => setAdminFullname(e.target.value)}
          disabled={submitting}
        />
        <FormField
          id="adminEmail"
          label="Email"
          type="email"
          required
          maxLength={150}
          value={adminEmail}
          onChange={(e) => setAdminEmail(e.target.value)}
          disabled={submitting}
        />

        {error && (
          <div
            role="alert"
            className="text-sm text-destructive bg-destructive/10 border border-destructive/20 rounded-md px-3 py-2"
          >
            {error}
          </div>
        )}

        <div className="flex items-center gap-3 pt-2">
          <Button type="submit" disabled={submitting}>
            {submitting ? "Creating…" : "Create Organisation"}
          </Button>
          <Button
            type="button"
            variant="outline"
            onClick={() => navigate("/")}
            disabled={submitting}
          >
            Cancel
          </Button>
        </div>
      </form>
    </div>
  );
}
