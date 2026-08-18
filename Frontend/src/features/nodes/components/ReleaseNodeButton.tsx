import { useState } from "react";
import { releaseNode } from "@/api/endpoints/nodes";
import { ApiError } from "@/api/client";
import { Button } from "@/components/ui/button";
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
  AlertDialogTrigger,
} from "@/components/ui/alert-dialog";

interface Props {
  id: string;
  alias: string;
  onReleased: () => void;
}

export function ReleaseNodeButton({ id, alias, onReleased }: Props) {
  const [open, setOpen] = useState(false);
  const [releasing, setReleasing] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const handleRelease = async () => {
    setReleasing(true);
    setError(null);
    try {
      await releaseNode(id);
      setOpen(false);
      onReleased();
    } catch (err) {
      setError(
        err instanceof ApiError ? err.message : "An unexpected error occurred.",
      );
    } finally {
      setReleasing(false);
    }
  };

  return (
    <AlertDialog open={open} onOpenChange={setOpen}>
      <AlertDialogTrigger render={<Button variant="destructive" size="sm" />}>
        Release
      </AlertDialogTrigger>
      <AlertDialogContent>
        <AlertDialogHeader>
          <AlertDialogTitle>Releasing node {alias}</AlertDialogTitle>
          <AlertDialogDescription>
            Releasing a device will remove it from the organisation and delete
            all associated data. This action can be undone but the data won't be
            restored.
          </AlertDialogDescription>
        </AlertDialogHeader>
        {error && (
          <p
            role="alert"
            className="text-sm text-destructive bg-destructive/10 border-destructive/20 rounded-md px-3 py-2"
          >
            {error}
          </p>
        )}
        <AlertDialogFooter>
          <AlertDialogCancel disabled={releasing}>Cancel</AlertDialogCancel>
          <AlertDialogAction
            onClick={(e) => {
              e.preventDefault();
              handleRelease();
            }}
            disabled={releasing}
            className="bg-destructive text-white hover:bg-destructive/90"
          >
            {releasing ? "Releasing..." : "Release"}
          </AlertDialogAction>
        </AlertDialogFooter>
      </AlertDialogContent>
    </AlertDialog>
  );
}
