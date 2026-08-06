import { Link } from "react-router";
import { useUsers } from "../hooks/useUsers";
import { AsyncBoundary } from "@/components/AsyncBoundary";
import { RoleBadge } from "@/components/RoleBadge";
import { useSetTopBar } from "@/components/layout/TopBarContext";
import { Button } from "@/components/ui/button";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import { Badge } from "@/components/ui/badge";

function formatDate(iso: string): string {
  return new Date(iso).toLocaleDateString("es-AR", {
    day: "2-digit",
    month: "2-digit",
    year: "numeric",
  });
}

export function UsersListPage() {
  const { users, loading, error } = useUsers();

  useSetTopBar(
    "Users List",
    <Button variant="outline" size="sm">
      <Link to="/users/new">Create User</Link>
    </Button>,
  );

  return (
    <AsyncBoundary
      loading={loading}
      error={error}
      isEmpty={users.length === 0}
      emptyMessage="No users found."
    >
      <div className="border rounded-lg bg-card">
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead>Name</TableHead>
              <TableHead>Email</TableHead>
              <TableHead>Role</TableHead>
              <TableHead>Active</TableHead>
              <TableHead>Created</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {users.map((u) => (
              <TableRow key={u.id}>
                <TableCell className="font-medium">
                  <div className="flex items-center gap-2">
                    {u.fullname}
                    {!u.emailVerified && (
                      <Badge
                        variant="outline"
                        className="text-amber-600 border-amber-300"
                      >
                        Unverified
                      </Badge>
                    )}
                  </div>
                </TableCell>
                <TableCell className="text-muted-foreground">
                  {u.email}
                </TableCell>
                <TableCell>
                  <RoleBadge role={u.role} />
                </TableCell>
                <TableCell>
                  <Badge variant={u.active ? "default" : "secondary"}>
                    {u.active ? "Active" : "Inactive"}
                  </Badge>
                </TableCell>
                <TableCell className="text-muted-foreground">
                  {formatDate(u.createdAt)}
                </TableCell>
                <TableCell className="text-right">
                  <Button asChild variant="ghost" size="sm">
                    <Link to={`/users/${u.id}/edit`}>Edit</Link>
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
