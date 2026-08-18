import { useParams, useNavigate } from "react-router";
import { useAuth } from "@/features/auth/AuthContext";
import { useGateway } from "../hooks/useGateway";
import { useSaveGateway } from "../hooks/useSaveGateway";
import { useSetTopBar } from "@/components/layout/TopBarContext";
import { AsyncBoundary } from "@/components/AsyncBoundary";
import {
  GatewayForm,
  parseCoords,
  type GatewayFormValues,
} from "../components/GatewayForm";
import { DeleteGatewayButton } from "../components/DeleteGatewayButton";
import { GatewayReadOnlyView } from "../components/GatewayReadOnlyView";
import { canModifyGateway } from "../permissions";

export function EditGatewayPage() {
  const { eui } = useParams<{ eui: string }>();
  const navigate = useNavigate();
  const { user } = useAuth();
  const { gateway, loading, error: loadError } = useGateway(eui);
  const { update, saving, error: saveError } = useSaveGateway();

  const canModify = gateway ? canModifyGateway(user?.role, gateway) : false;

  useSetTopBar(canModify ? "Edit Gateway" : "View Gateway");

  const handleSubmit = async (values: GatewayFormValues) => {
    if (!eui) return;
    const { latitude, longitude } = parseCoords(values);
    const result = await update(eui, {
      alias: values.alias,
      model: values.model,
      operativeState: values.operativeState,
      latitude,
      longitude,
    });
    if (result) navigate("/gateways");
  };

  return (
    <div className="max-w-xl">
      <AsyncBoundary loading={loading} error={loadError}>
        {gateway &&
          (canModify ? (
            <div className="space-y-8">
              <GatewayForm
                mode="edit"
                initial={gateway}
                saving={saving}
                error={saveError}
                onSubmit={handleSubmit}
                onCancel={() => navigate("/gateways")}
              />
              <div className="border border-destructive/30 rounded-lg p-4">
                <h3 className="text-sm font-medium text-foreground mb-1">
                  Delete Gateway
                </h3>
                <p className="text-xs text-muted-foreground mb-3">
                  The gateway will be permanently deleted.
                </p>
                <DeleteGatewayButton
                  eui={gateway.gatewayEui}
                  alias={gateway.alias}
                  onDeleted={() => navigate("/gateways")}
                />
              </div>
            </div>
          ) : (
            <GatewayReadOnlyView
              gateway={gateway}
              onBack={() => navigate("/gateways")}
            />
          ))}
      </AsyncBoundary>
    </div>
  );
}
