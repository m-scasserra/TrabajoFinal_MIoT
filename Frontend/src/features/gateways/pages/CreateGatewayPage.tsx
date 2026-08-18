import { useNavigate } from "react-router";
import { useSaveGateway } from "../hooks/useSaveGateway";
import { useSetTopBar } from "@/components/layout/TopBarContext";
import {
  GatewayForm,
  parseCoords,
  type GatewayFormValues,
} from "../components/GatewayForm";

export function CreateGatewayPage() {
  const navigate = useNavigate();
  const { create, saving, error } = useSaveGateway();

  useSetTopBar("New Gateway");

  const handleSubmit = async (values: GatewayFormValues) => {
    const { latitude, longitude } = parseCoords(values);
    const result = await create({
      gatewayEui: values.gatewayEui,
      alias: values.alias,
      model: values.model,
      latitude,
      longitude,
    });
    if (result) navigate("/gateways");
  };

  return (
    <div className="max-w-2xl">
      <GatewayForm
        mode="create"
        saving={saving}
        error={error}
        onSubmit={handleSubmit}
        onCancel={() => navigate("/gateways")}
      />
    </div>
  );
}
