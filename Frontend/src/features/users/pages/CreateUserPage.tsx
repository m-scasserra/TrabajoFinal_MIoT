import { useState, type FormEvent } from "react";
import { useNavigate } from "react-router";
import { useAuth } from "../../auth/AuthContext";
import { assignableRoles, roleLabels } from "../roles";
import { useCreateUser } from "../hooks/useCreateUser";
import { useSetTopBar } from "@/components/layout/TopBarContext";
import { FormField } from "@/components/FormField";
import { Button } from "@/components/ui/button";
import { Label } from "@/components/ui/label";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";

export function CreateUserPage() {
  const { user } = useAuth();
  const navigate = useNavigate();
  const { submit, submitting, error } = useCreateUser();

  useSetTopBar("Create User");

  const roles = assignableRoles(user?.role ?? "");

  const [fullname, setFullname] = useState("");
  const [email, setEmail] = useState("");
  const [role, setRole] = useState<string>(roles[0] ?? "");

  if (roles.length === 0) {
    return (
      <p className="text-sm text-destructive bg-destructive/10 border border-destructive/20 rounded-md px-4 py-3">
        You do not have permission to create users.
      </p>
    );
  }

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault();
    const ok = await submit({ fullname, email, role });
    if (ok) navigate("/users");
  };

  return (
    <div className="max-w-md">
      <form onSubmit={handleSubmit} className="space-y-4" noValidate>
        <FormField
          id="fullname"
          label="Full Name"
          type="text"
          required
          maxLength={150}
          value={fullname}
          onChange={(e) => setFullname(e.target.value)}
          disabled={submitting}
        />
        <FormField
          id="email"
          label="Email"
          type="email"
          required
          maxLength={150}
          value={email}
          onChange={(e) => setEmail(e.target.value)}
          disabled={submitting}
        />

        <div className="space-y-1.5">
          <label htmlFor="role">Role</label>
          <Select value={role} onValueChange={setRole} disabled={submitting}>
            <SelectTrigger id="role" className="w-full">
              <SelectValue placeholder="Select a role" />
            </SelectTrigger>
            <SelectContent>
              {roles.map((r) => (
                <SelectItem key={r} value={r}>
                  {roleLabels[r] ?? r}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
        </div>

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
            {submitting ? "Creating..." : "Create User"}
          </Button>
          <Button
            type="button"
            variant="outline"
            onClick={() => navigate("/users")}
            disabled={submitting}
          >
            Cancel
          </Button>
        </div>
      </form>
    </div>
  );
}
