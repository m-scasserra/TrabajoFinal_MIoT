import { isGatewayOnline } from "../constants";
import { cn } from "@/lib/utils";

function relativeTime(iso: string): string {
  const diff = Date.now() - new Date(iso).getTime();
  const mins = Math.floor(diff / 60000);
  if (mins < 1) return "just now";
  if (mins < 60) return `${mins} minutes ago`;
  const hours = Math.floor(mins / 60);
  if (hours < 24) return `${hours} hours ago`;
  const days = Math.floor(hours / 24);
  return `${days} days ago`;
}

export function GatewayPresence({ lastSeen }: { lastSeen: string | null }) {
  const online = isGatewayOnline(lastSeen);
  return (
    <div className="flex items-center gap-2">
      <span
        className={cn(
          "h-2 w-2 rounded-full",
          online ? "bg-green-500" : "bg-gray-300",
        )}
      />
      <span className="text-sm text-muted-foreground">
        {lastSeen ? relativeTime(lastSeen) : "never seen"}
      </span>
    </div>
  );
}
