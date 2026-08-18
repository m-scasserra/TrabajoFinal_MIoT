import { useState, type FormEvent } from "react";
import { useNavigate } from "react-router";
import { useAuth } from "@/features/auth/AuthContext";
import { Roles } from "@/features/users/roles";
import { useProvisionNode } from "../hooks/useProvisionNode";
import { useSetTopBar } from "@/components/layout/TopBarContext";
import { FormField } from "@/components/FormField";
import { AppKeyReveal } from "../components/AppKeyReveal";
import { Button } from "@/components/ui/button";
import type { ProvisionNodeResponse } from "@/api/endpoints/nodes";

function Field({
  label,
  children,
}: {
  label: string;
  children: React.ReactNode;
}) {
  return (
    <div className="space-y-1">
      <p className="text-xs text-muted-foreground">{label}</p>
      <div className="text-sm text-foreground">{children}</div>
    </div>
  );
}

export function ProvisionNodePage() {
  const { user } = useAuth();
  const navigate = useNavigate();
  const { provision, provisioning, error } = useProvisionNode();

  useSetTopBar("Provision device");

  const [macAddress, setMacAddress] = useState("");
  const [hwRevision, setHwRevision] = useState("");
  const [fwRevision, setFwRevision] = useState("");
  const [result, setResult] = useState<ProvisionNodeResponse | null>(null);

  if (user?.role !== Roles.SuperAdmin) {
    return (
      <p className="text-sm text-destructive bg-destructive/10 border border-destructive/20 rounded-md px-4 py-3">
        You do not have permission to provision a device.
      </p>
    );
  }

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault();
    const res = await provision({
      macAddress: macAddress.trim().toLowerCase(),
      hwRevision: Number(hwRevision),
      fwRevision: Number(fwRevision),
    });
    if (res) setResult(res);
  };

  if (result) {
    return (
      <div className="max-w-xl space-y-6">
        <div className="grid grid-cols-2 gap-4 rounded-lg border bg-card p-4">
          <Field label="Dev EUI">
            <span className="font-mono text-xs">{result.devEui}</span>
          </Field>
          <Field label="MAC">
            <span className="font-mono text-xs">{result.macAddress}</span>
          </Field>
          <Field label="Hardware revision">{result.hwRevision}</Field>
          <Field label="Firmware revision">{result.fwRevision}</Field>
        </div>

        <AppKeyReveal
          appKey={result.appKey}
          onDone={() => navigate("/provisioning/nodes")}
        />
      </div>
    );
  }

  return (
    <div className="max-w-xl">
      <p className="text-sm text-muted-foreground mb-5">
        Provisioning a device will generate a new Dev EUI and App Key for the
        device. The device will be added to the list of provisioned devices and
        can be used to connect to the network. The App Key will only be shown
        once, so make sure to copy it before proceeding.
      </p>

      <form onSubmit={handleSubmit} className="space-y-4" noValidate>
        <FormField
          id="macAddress"
          label="MAC Address"
          required
          value={macAddress}
          onChange={(e) => setMacAddress(e.target.value.trim())}
          disabled={provisioning}
          placeholder="12 hex digits, e.g., 01:23:45:67:89:AB"
          pattern="[0-9A-Fa-f]{12}"
          maxLength={12}
        />
        <div className="grid grid-cols-2 gap-4">
          <FormField
            id="hwRevision"
            label="Hardware Revision"
            type="number"
            required
            min={0}
            max={255}
            value={hwRevision}
            onChange={(e) => setHwRevision(e.target.value)}
            disabled={provisioning}
          />
          <FormField
            id="fwRevision"
            label="Firmware Revision"
            type="number"
            required
            min={0}
            max={255}
            value={fwRevision}
            onChange={(e) => setFwRevision(e.target.value)}
            disabled={provisioning}
          />
        </div>

        {error && (
          <p className="text-sm text-destructive bg-destructive/10 border border-destructive/20 rounded-md px-3 py-2">
            {error}
          </p>
        )}

        <div className="flex items-center gap-3 pt-1">
          <Button type="submit" disabled={provisioning}>
            {provisioning ? "Provisioning..." : "Provision"}
          </Button>
          <Button
            type="button"
            variant="outline"
            onClick={() => navigate("/provisioning/nodes")}
            disabled={provisioning}
          >
            Cancel
          </Button>
        </div>
      </form>
    </div>
  );
}
