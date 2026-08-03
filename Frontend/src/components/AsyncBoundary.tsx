import type { ReactNode } from "react";

interface AsyncBoundaryProps {
  loading: boolean;
  error: string | null;
  isEmpty?: boolean;
  emptyMessage?: string;
  children: ReactNode;
}

export function AsyncBoundary({
  loading,
  error,
  isEmpty,
  emptyMessage = "No data available",
  children,
}: AsyncBoundaryProps) {
  if (loading) {
    return (
      <div className="text-sm text-muted-foreground py-8 text-center">
        Loading...
      </div>
    );
  }
  if (error) {
    return (
      <div
        role="alert"
        className="text-sm text-destructive bg-destructive/10 border border-destructive/20 rounded-lg px-4 py-3"
      >
        {error}
      </div>
    );
  }
  if (isEmpty) {
    return (
      <div className="text-sm text-muted-foreground py-8 text-center border border-dashed rounded-lg">
        {emptyMessage}
      </div>
    );
  }
  return <>{children}</>;
}
