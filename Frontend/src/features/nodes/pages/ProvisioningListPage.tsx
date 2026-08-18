import { Link } from "react-router";
import { useProvisionedNodes } from "../hooks/useProvisionedNodes";
import { useAuth } from "@/features/auth/AuthContext";
import { Roles } from "@/features/users/roles";
import { useSetTopBar } from "@/components/layout/TopBarContext";
import { AsyncBoundary } from "@/components/AsyncBoundary";
import { SyncStatusBadge } from "@/components/SyncStatusBadge";
import { RotateKeyButton } from "../components/RotateKeyButton";
import { Button } from "@/components/ui/button";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";

export function ProvisioningListPage() {
  const { user } = useAuth();
  const { nodes, loading, error, reload } = useProvisionedNodes();

  useSetTopBar(
    "Provisioned devices",
    <div className="flex items-center gap-2">
      <Button variant="outline" size="sm" onClick={reload}>
        Reload
      </Button>
      <Button asChild size="sm">
        <Link to="/provisioning/nodes/new">Provision device</Link>
      </Button>
    </div>,
  );

  if (user?.role !== Roles.SuperAdmin) {
    return (
      <p className="text-sm text-destructive bg-destructive/10 border border-destructive/20 rounded-md px-4 py-3">
        You do not have permission to view this page.
      </p>
    );
  }

  return (
    <AsyncBoundary
      loading={loading}
      error={error}
      isEmpty={nodes.length === 0}
      emptyMessage="No provisioned devices found."
    >
      <div className="border rounded-lg bg-card">
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead>MAC</TableHead>
              <TableHead>Dev EUI</TableHead>
              <TableHead>HW</TableHead>
              <TableHead>FW</TableHead>
              <TableHead>Sync</TableHead>
              <TableHead className="w-56" />
            </TableRow>
          </TableHeader>
          <TableBody>
            {nodes.map((n) => (
              <TableRow key={n.devEui}>
                <TableCell className="font-mono text-xs">
                  {n.macAddress}
                </TableCell>
                <TableCell className="font-mono text-xs text-muted-foreground">
                  {n.devEui}
                </TableCell>
                <TableCell className="text-muted-foreground">
                  {n.hwRevision}
                </TableCell>
                <TableCell className="text-muted-foreground">
                  {n.fwRevision}
                </TableCell>
                <TableCell>
                  <SyncStatusBadge
                    status={n.syncStatus}
                    syncError={n.syncError}
                  />
                </TableCell>
                <TableCell>
                  <div className="flex items-center justify-end gap-2">
                    <RotateKeyButton id={n.id} alias={n.macAddress} />
                    <Button asChild variant="ghost" size="sm">
                      <Link
                        to={`/provisioning/nodes/${n.id}/edit`}
                        state={{
                          alias: n.macAddress,
                          hwRevision: n.hwRevision,
                          fwRevision: n.fwRevision,
                        }}
                      >
                        Edit
                      </Link>
                    </Button>
                  </div>
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      </div>
    </AsyncBoundary>
  );
}
