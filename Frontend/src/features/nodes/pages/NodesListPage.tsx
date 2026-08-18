import { Link } from "react-router";
import { useNodes } from "../hooks/useNodes";
import { meterTypeLabels } from "../constants";
import { operativeStateLabels } from "../../gateways/constants";
import { useSetTopBar } from "@/components/layout/TopBarContext";
import { AsyncBoundary } from "@/components/AsyncBoundary";
import { SyncStatusBadge } from "@/components/SyncStatusBadge";
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

export function NodesListPage() {
  const { nodes, loading, error, reload } = useNodes();

  useSetTopBar(
    "Devices",
    <div className="flex items-center gap-2">
      <Button variant="outline" size="sm" onClick={reload}>
        Refresh
      </Button>
      <Button asChild size="sm">
        <Link to="/nodes/activate">Activate device</Link>
      </Button>
    </div>,
  );

  return (
    <AsyncBoundary
      loading={loading}
      error={error}
      isEmpty={nodes.length === 0}
      emptyMessage="No devices found."
    >
      <div className="border rounded-lg bg-card">
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead>Alias</TableHead>
              <TableHead>Dev EUI</TableHead>
              <TableHead>Type</TableHead>
              <TableHead>Status</TableHead>
              <TableHead>Sync</TableHead>
              <TableHead className="w-24" />
            </TableRow>
          </TableHeader>
          <TableBody>
            {nodes.map((n) => (
              <TableRow key={n.devEui}>
                <TableCell className="font-medium">{n.alias}</TableCell>
                <TableCell className="font-mono text-xs text-muted-foreground">
                  {n.devEui}
                </TableCell>
                <TableCell className="text-muted-foreground">
                  {n.meterType ? meterTypeLabels[n.meterType] : "-"}
                </TableCell>
                <TableCell>
                  <Badge variant="outline">
                    {operativeStateLabels[n.operativeState]}
                  </Badge>
                </TableCell>
                <TableCell>
                  <SyncStatusBadge
                    status={n.syncStatus}
                    syncError={n.syncError}
                  />
                </TableCell>
                <TableCell className="text-right">
                  <Button asChild variant="ghost" size="sm">
                    <Link to={`/nodes/${n.devEui}`}>Details</Link>
                  </Button>
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      </div>
    </AsyncBoundary>
  );
}
