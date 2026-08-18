import { Badge } from "@/components/ui/badge";
import { isSystemGateway } from "../permissions";
import type { Gateway } from "@/api/endpoints/gateways";

export function GatewayOwnerBadge({
  gateway,
}: {
  gateway: Pick<Gateway, "orgId">;
}) {
  if (!isSystemGateway(gateway)) return null;
  return (
    <Badge variant="secondary" className="gap-1">
      System
    </Badge>
  );
}
