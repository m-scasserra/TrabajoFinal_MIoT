import { Badge } from "@/components/ui/badge";
import {
  Tooltip,
  TooltipContent,
  TooltipTrigger,
} from "@/components/ui/tooltip";
import type { SyncStatus } from "@/api/endpoints/deviceProfiles";

interface SyncMeta {
  label: string;
  className: string;
}

const SYNC_META: Record<SyncStatus, SyncMeta> = {
  PENDING: {
    label: "Pending",
    className: "bg-amber-50 text-amber-700 border-amber-200",
  },
  SYNCED: {
    label: "Synced",
    className: "bg-green-50 text-green-700 border-green-200",
  },
  FAILED: {
    label: "Fail on sync",
    className: "bg-red-50 text-red-700 border-red-200",
  },
  PENDING_DELETE: {
    label: "Deleting",
    className: "bg-muted text-muted-foreground border-border",
  },
  DELETE_FAILED: {
    label: "Delete failed",
    className: "bg-red-50 text-red-700 border-red-200",
  },
};

interface SyncStatusBadgeProps {
  status: SyncStatus;
  syncError?: string | null;
}

export function SyncStatusBadge({ status, syncError }: SyncStatusBadgeProps) {
  const meta = SYNC_META[status] ?? SYNC_META.PENDING;
  const badge = (
    <Badge variant="outline" className={meta.className}>
      {meta.label}
    </Badge>
  );

  const isError = status === "FAILED" || status === "DELETE_FAILED";
  if (isError && syncError) {
    return (
      <Tooltip>
        <TooltipTrigger asChild>
          <span className="cursor-help">{badge}</span>
        </TooltipTrigger>
        <TooltipContent className="max-w-xs">{syncError}</TooltipContent>
      </Tooltip>
    );
  }

  return badge;
}
