import { Badge } from "@/components/ui/badge";
import { roleLabels } from "@/features/users/roles";

export function RoleBadge({ role }: { role: string }) {
  return <Badge variant="outline">{roleLabels[role] ?? role}</Badge>;
}
