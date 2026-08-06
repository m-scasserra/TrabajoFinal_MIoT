import { useState, useEffect, type FormEvent } from "react";
import { useParams, useNavigate } from "react-router";
import { useAuth } from "../../auth/AuthContext";
import { useUser } from "../hooks/useUser";
import { useUpdateUser } from "../hooks/useUpdateUser";
import { useSetTopBar } from "@/components/layout/TopBarContext";
import { AsyncBoundary } from "@/components/AsyncBoundary";
import { FormField } from "@/components/FormField";
import { RoleBadge } from "@/components/RoleBadge";
import { Button } from "@/components/ui/button";
import { Label } from "@/components/ui/label";
import { Switch } from "@/components/ui/switch";

export function EditUserPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const { user: me } = useAuth();
  const { user, loading, error: loadError } = useUser(id);
  const { submit, submitting, error: saveError } = useUpdateUser();

  useSetTopBar("Edit User");

  const [fullname, setFullname] = useState("");
  const [active, setActive] = useState(true);

  useEffect(() => {
    if (user) {
      setFullname(user.fullname);
      setActive(user.active);
    }
  }, [user]);

  const isSelf = me?.id === id;

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault();
    if (!id) return;
    const ok = await submit(id, { fullname, active });
    if (ok) navigate("/users");
  };

  return (
    <div className="max-w-md">
      <AsyncBoundary loading={loading} error={loadError}>
        {user && (
          <form onSubmit={handleSubmit} className="space-y-5" noValidate>
            <div className="space-y-1.5">
              <Label>Email</Label>
              <p className="text-sm text-muted-foreground">{user.email}</p>
            </div>

            <div className="space-y-1.5">
              <Label>Role</Label>
              <div>
                <RoleBadge role={user.role} />
              </div>
            </div>

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

            <div className="flex items-center justify-between rounded-lg border p-3">
              <div className="space-y-0.5">
                <Label htmlFor="active">Active</Label>
                <p className="text-xs text-muted-foreground">
                  {isSelf
                    ? "You cannot deactivate your own account."
                    : "If inactive, the user will not be able to log in."}
                </p>
              </div>
              <Switch
                id="active"
                checked={active}
                onCheckedChange={setActive}
                disabled={isSelf || submitting}
              />
            </div>

            {saveError && (
              <p
                role="alert"
                className="text-sm text-destructive bg-destructive/10 border border-destructive/20 rounded-md px-3 py-2"
              >
                {saveError}
              </p>
            )}

            <div className="flex items-center gap-3 pt-1">
              <Button type="submit" disabled={submitting}>
                {submitting ? "Saving…" : "Save Changes"}
              </Button>
              <Button
                type="button"
                variant="outline"
                onClick={() => navigate("/users")}
              >
                Cancel
              </Button>
            </div>
          </form>
        )}
      </AsyncBoundary>
    </div>
  );
}
