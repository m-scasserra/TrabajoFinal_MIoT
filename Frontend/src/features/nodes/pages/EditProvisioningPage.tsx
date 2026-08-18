import { useState, type FormEvent } from "react";
import { useParams, useNavigate, useLocation } from "react-router";
import { useAuth } from "@/features/auth/AuthContext";
import { Roles } from "@/features/users/roles";
import { updateProvisioning } from "@/api/endpoints/nodes";
import { ApiError } from "@/api/client";
import { useSetTopBar } from "@/components/layout/TopBarContext";
import { FormField } from "@/components/FormField";
import { RotateKeyButton } from "../components/RotateKeyButton";
import { Button } from "@/components/ui/button";

export function EditProvisioningPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const location = useLocation();
  const { user } = useAuth();

  useSetTopBar("Edit Provisioning");

  const navState = location.state as {
    alias?: string;
    hwRevision?: number;
    fwRevision?: number;
  } | null;
  const alias = navState?.alias ?? "device";

  const [hwRevision, setHwRevision] = useState(
    navState?.hwRevision != null ? String(navState.hwRevision) : "",
  );
  const [fwRevision, setFwRevision] = useState(
    navState?.fwRevision != null ? String(navState.fwRevision) : "",
  );
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);

  if (user?.role !== Roles.SuperAdmin) {
    return (
      <p className="text-sm text-destructive bg-destructive/10 border border-destructive/20 rounded-md px-4 py-3">
        You do not have permission to edit provisioning.
      </p>
    );
  }

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault();
    if (!id) return;
    setSaving(true);
    setError(null);
    try {
      await updateProvisioning(id, {
        hwRevision: Number(hwRevision),
        fwRevision: Number(fwRevision),
      });
      navigate("/provisioning/nodes");
    } catch (err) {
      if (err instanceof ApiError && err.status === 404) {
        setError(
          "Could not modify provisioning. The device may have already been activated or deleted.",
        );
      } else if (err instanceof ApiError) {
        setError(err.message);
      } else {
        setError("An unexpected error occurred");
      }
    } finally {
      setSaving(false);
    }
  };

  return (
    <div className="max-w-xl space-y-8">
      <div>
        <p className="text-sm text-muted-foreground mb-5">
          Edit the provisioning details for {alias}. This remakes the Device EUI
          and App Key, so make sure to update any applications that use this
          device.
        </p>
        <form onSubmit={handleSubmit} className="space-y-4" noValidate>
          <div className="grid grid-cols-2 gap-4">
            <FormField
              id="hwRevision"
              label="Hardware revision"
              type="number"
              required
              min={0}
              max={255}
              value={hwRevision}
              onChange={(e) => setHwRevision(e.target.value)}
              disabled={saving}
            />
            <FormField
              id="fwRevision"
              label="Firmware revision"
              type="number"
              required
              min={0}
              max={255}
              value={fwRevision}
              onChange={(e) => setFwRevision(e.target.value)}
              disabled={saving}
            />
          </div>

          {error && (
            <p
              role="alert"
              className="text-sm text-destructive bg-destructive/10 border border-destructive/20 rounded-md px-3 py-2"
            >
              {error}
            </p>
          )}

          <div className="flex items-center gap-3 pt-1">
            <Button type="submit" disabled={saving}>
              {saving ? "Saving..." : "Save"}
            </Button>
            <Button
              type="button"
              variant="outline"
              onClick={() => navigate("/provisioning/nodes")}
              disabled={saving}
            >
              Cancel
            </Button>
          </div>
        </form>
      </div>

      {id && (
        <div className="border rounded-lg p-4">
          <h3 className="text-sm font-medium text-foreground mb-1">
            Rotate Device Key
          </h3>
          <p className="text-xs text-muted-foreground mb-3">
            Rotating the device key will invalidate the current key. Make sure
            to update any applications that use this key.
          </p>
          <RotateKeyButton id={id} alias={alias} />
        </div>
      )}
    </div>
  );
}
