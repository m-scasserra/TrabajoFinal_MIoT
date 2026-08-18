import { useState } from "react";
import { rotateNodeKey } from "@/api/endpoints/nodes";
import { ApiError } from "@/api/client";
import { Button } from "@/components/ui/button";
import { AppKeyReveal } from "./AppKeyReveal";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
} from "@/components/ui/dialog";

interface Props {
  id: string;
  alias: string;
}

export function RotateKeyButton({ id, alias }: Props) {
  const [open, setOpen] = useState(false);
  const [rotating, setRotating] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [newKey, setNewKey] = useState<string | null>(null);

  const handleRotate = async () => {
    setRotating(true);
    setError(null);
    try {
      const res = await rotateNodeKey(id);
      setNewKey(res.appKey);
    } catch (err) {
      setError(
        err instanceof ApiError ? err.message : "An unexpected error occurred",
      );
    } finally {
      setRotating(false);
    }
  };

  const handleClose = () => {
    setOpen(false);
    setNewKey(null);
    setError(null);
  };

  return (
    <Dialog
      open={open}
      onOpenChange={(o) => (o ? setOpen(true) : handleClose())}
    >
      <DialogTrigger render={<Button variant="outline" size="sm" />}>
        Rotate Key
      </DialogTrigger>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>Rotate Key for {alias}</DialogTitle>
          <DialogDescription>
            Generating a new key will invalidate the current key. Make sure to
            update any applications that use this key.
          </DialogDescription>
        </DialogHeader>

        {newKey ? (
          <AppKeyReveal appKey={newKey} onDone={handleClose} />
        ) : (
          <>
            {error && (
              <p
                role="alert"
                className="text-sm text-destructive bg-destructive/10 border border-destructive/20 rounded-md px-3 py-2"
              >
                {error}
              </p>
            )}
            <DialogFooter>
              <Button
                variant="outline"
                onClick={handleClose}
                disabled={rotating}
              >
                Cancel
              </Button>
              <Button onClick={handleRotate} disabled={rotating}>
                {rotating ? "Rotating..." : "Rotate Key"}
              </Button>
            </DialogFooter>
          </>
        )}
      </DialogContent>
    </Dialog>
  );
}
