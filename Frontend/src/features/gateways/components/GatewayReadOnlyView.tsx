import type { Gateway } from "@/api/endpoints/gateways";
import { operativeStateLabels } from "../constants";
import { SyncStatusBadge } from "@/components/SyncStatusBadge";
import { GatewayPresence } from "./GatewayPresence";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";

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

export function GatewayReadOnlyView({
  gateway,
  onBack,
}: {
  gateway: Gateway;
  onBack: () => void;
}) {
  return (
    <div className="space-y-6">
      <div className="rounded-lg border bg-muted/30 px-4 py-3 text-sm text-muted-foreground">
        This is a system gateway ( shared infrastructure ). You can only view
        its details. Contact your administrator if you need to make changes to
        this gateway.
      </div>

      <div className="grid grid-cols-2 gap-5">
        <Field label="Alias">{gateway.alias}</Field>
        <Field label="EUI">
          <span className="font-mono text-xs">{gateway.gatewayEui}</span>
        </Field>
        <Field label="Model">{gateway.model}</Field>
        <Field label="Operating State">
          <Badge variant="outline">
            {operativeStateLabels[gateway.operativeState]}
          </Badge>
        </Field>
        <Field label="Last Seen">
          <GatewayPresence lastSeen={gateway.lastSeen} />
        </Field>
        <Field label="Sync">
          <SyncStatusBadge
            status={gateway.syncStatus}
            syncError={gateway.syncError}
          />
        </Field>
        {gateway.latitude != null && gateway.longitude != null && (
          <Field label="Location">
            <span className="font-mono text-xs">
              {gateway.latitude}, {gateway.longitude}
            </span>
          </Field>
        )}
      </div>
      <Button variant="outline" onClick={onBack}>
        Back
      </Button>
    </div>
  );
}
