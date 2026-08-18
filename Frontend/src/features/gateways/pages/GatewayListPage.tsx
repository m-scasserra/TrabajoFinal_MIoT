import { Link } from "react-router";
import { useAuth } from "@/features/auth/AuthContext";
import { canModifyGateway } from "../permissions";
import { GatewayOwnerBadge } from "../components/GatewayOwnerBadge";
import { useGateways } from "../hooks/useGateways";
import { operativeStateLabels } from "../constants";
import { useSetTopBar } from "@/components/layout/TopBarContext";
import { AsyncBoundary } from "@/components/AsyncBoundary";
import { SyncStatusBadge } from "@/components/SyncStatusBadge";
import { GatewayPresence } from "../components/GatewayPresence";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";

export function GatewayListPage() {
  const { user } = useAuth();
  const { gateways, loading, error, reload } = useGateways();

  useSetTopBar(
    "Gateways",
    <div className="flex items-center gap-2">
      <Button variant="outline" size="sm" onClick={reload}>
        Reload
      </Button>
      <Button asChild size="sm">
        <Link to="/gateways/new">New Gateway</Link>
      </Button>
    </div>,
  );

  return (
    <AsyncBoundary
      loading={loading}
      error={error}
      isEmpty={gateways.length === 0}
      emptyMessage="No gateways found."
    >
      <div className="border rounded-lg bg-card">
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead>Alias</TableHead>
              <TableHead>Eui</TableHead>
              <TableHead>Model</TableHead>
              <TableHead>Status</TableHead>
              <TableHead>Last seen</TableHead>
              <TableHead>Sync</TableHead>
              <TableHead className="w-24" />
            </TableRow>
          </TableHeader>
          <TableBody>
            {gateways.map((g) => (
              <TableRow key={g.gatewayEui}>
                <TableCell className="font-medium">
                  <div className="flex items-center gap-2">
                    {g.alias}
                    <GatewayOwnerBadge gateway={g} />
                  </div>
                </TableCell>
                <TableCell className="font-mono text-xs text-mute-foreground">
                  {g.gatewayEui}
                </TableCell>
                <TableCell className="text-muted-foreground">
                  {g.model}
                </TableCell>
                <TableCell>
                  <Badge variant="outline">
                    {operativeStateLabels[g.operativeState]}
                  </Badge>
                </TableCell>
                <TableCell>
                  <GatewayPresence lastSeen={g.lastSeen} />
                </TableCell>
                <TableCell>
                  <SyncStatusBadge
                    status={g.syncStatus}
                    syncError={g.syncError}
                  />
                </TableCell>
                <TableCell className="text-right">
                  {canModifyGateway(user?.role, g) ? (
                    <Button asChild variant="ghost" size="sm">
                      <Link to={`/gateways/${g.gatewayEui}/edit`}>Edit</Link>
                    </Button>
                  ) : (
                    <Button asChild variant="ghost" size="sm">
                      <Link to={`/gateways/${g.gatewayEui}/edit`}>Details</Link>
                    </Button>
                  )}
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      </div>
    </AsyncBoundary>
  );
}
