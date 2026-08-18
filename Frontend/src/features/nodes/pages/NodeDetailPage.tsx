import { useParams, useNavigate } from "react-router";
import { useNode } from "../hooks/useNode";
import { meterTypeLabels } from "../constants";
import { operativeStateLabels } from "@/features/gateways/constants";
import { useSetTopBar } from "@/components/layout/TopBarContext";
import { AsyncBoundary } from "@/components/AsyncBoundary";
import { SyncStatusBadge } from "@/components/SyncStatusBadge";
import { ReleaseNodeButton } from "../components/ReleaseNodeButton";
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

export function NodeDetailPage() {
  const { devEui } = useParams<{ devEui: string }>();
  const navigate = useNavigate();
  const { node, loading, error } = useNode(devEui);

  useSetTopBar("Device details");

  return (
    <div className="max-w-xl">
      <AsyncBoundary loading={loading} error={error}>
        {node && (
          <div className="space-y-8">
            <div className="grid grid-cols-2 gap-5">
              <Field label="Alias">{node.alias}</Field>
              <Field label="Dev EUI">
                <span className="font-mono text-xs">{node.devEui}</span>
              </Field>
              <Field label="MAC">
                <span className="font-mono text-xs">{node.macAddress}</span>
              </Field>
              <Field label="Type">
                {node.meterType ? meterTypeLabels[node.meterType] : "-"}
              </Field>
              <Field label="State">
                <Badge variant="outline">
                  {operativeStateLabels[node.operativeState]}
                </Badge>
              </Field>
              <Field label="Sync">
                <SyncStatusBadge
                  status={node.syncStatus}
                  syncError={node.syncError}
                />
              </Field>
              <Field label="Hardware version">{node.hwRevision}</Field>
              <Field label="Software version">{node.fwRevision}</Field>
              {node.latitude != null && node.longitude != null && (
                <Field label="Location">
                  <span className="font-mono text-xs">
                    {node.latitude} {node.longitude}
                  </span>
                </Field>
              )}
            </div>

            <div className="border border-destructive/30 rounded-lg p-4">
              <h3 className="text-sm font-medium text-foreground mb-1">
                Release device
              </h3>
              <p className="text-xs text-muted-foreground mb-3">
                Releasing a device will remove it from the organisation and
                delete all associated data. This action can be undone but the
                data won't be restored.
              </p>
              <ReleaseNodeButton
                id={node.id}
                alias={node.alias}
                onReleased={() => navigate("/nodes")}
              />
            </div>
          </div>
        )}
      </AsyncBoundary>
    </div>
  );
}
