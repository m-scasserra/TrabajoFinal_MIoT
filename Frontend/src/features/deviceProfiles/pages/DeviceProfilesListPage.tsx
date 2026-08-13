import { Link } from "react-router";
import { useDeviceProfiles } from "../hooks/useDeviceProfiles";
import { useAuth } from "@/features/auth/AuthContext";
import { Roles } from "@/features/users/roles";
import { useSetTopBar } from "@/components/layout/TopBarContext";
import { AsyncBoundary } from "@/components/AsyncBoundary";
import { SyncStatusBadge } from "@/components/SyncStatusBadge";
import { Button } from "@/components/ui/button";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";

export function DeviceProfilesListPage() {
  const { user } = useAuth();
  const { profiles, loading, error, reload } = useDeviceProfiles();
  const isSuperAdmin = user?.role === Roles.SuperAdmin;

  useSetTopBar(
    "Device Profiles",
    <div className="flex items-center gap-2">
      <Button variant="outline" size="sm" onClick={reload}>
        Reload
      </Button>
      {isSuperAdmin && (
        <Button asChild size="sm">
          <Link to="/device-profiles/new">New Device Profile</Link>
        </Button>
      )}
    </div>,
  );

  return (
    <AsyncBoundary
      loading={loading}
      error={error}
      isEmpty={profiles.length === 0}
      emptyMessage="No device profiles found."
    >
      <div className="border rounded-lg bg-card">
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead>Name</TableHead>
              <TableHead>Region</TableHead>
              <TableHead>MAC version</TableHead>
              <TableHead>Uplink (s)</TableHead>
              <TableHead>Sync</TableHead>
              {isSuperAdmin && <TableHead className="w-24" />}
            </TableRow>
          </TableHeader>
          <TableBody>
            {profiles.map((p) => (
              <TableRow key={p.id}>
                <TableCell className="font-medium">{p.name}</TableCell>
                <TableCell className="text-muted-foreground">
                  {p.region}
                </TableCell>
                <TableCell className="text-muted-foreground">
                  {p.macVersion}
                </TableCell>
                <TableCell className="text-muted-foreground">
                  {p.uplinkInterval}
                </TableCell>
                <TableCell>
                  <SyncStatusBadge
                    status={p.syncStatus}
                    syncError={p.syncError}
                  />
                </TableCell>
                {isSuperAdmin && (
                  <TableCell className="text-right">
                    <Button asChild variant="ghost" size="sm">
                      <Link to={`/device-profiles/${p.id}`}>Edit</Link>
                    </Button>
                  </TableCell>
                )}
              </TableRow>
            ))}
          </TableBody>
        </Table>
      </div>
    </AsyncBoundary>
  );
}
