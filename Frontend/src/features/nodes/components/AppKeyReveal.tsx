import { useState } from "react";
import { Button } from "@/components/ui/button";
import { Check, Copy, TriangleAlert } from "lucide-react";

interface AppKeyRevealProps {
  appKey: string;
  onDone: () => void;
}

export function AppKeyReveal({ appKey, onDone }: AppKeyRevealProps) {
  const [copied, setCopied] = useState(false);

  const handleCopy = async () => {
    try {
      await navigator.clipboard.writeText(appKey);
      setCopied(true);
      setTimeout(() => setCopied(false), 2000);
    } catch {
      setCopied(false);
    }
  };

  return (
    <div className="rounded-lg border border-amber-300 bg-amber-50 p-5 space-y-4">
      <div className="flex items-start gap-3">
        <TriangleAlert className="h-5 w-5 text-amber-600 shrink-0 mt-0.5" />
        <div className="space-y-1">
          <h3 className="text-sm font-semibold text-amber-900">
            This is your device's App Key. Please copy it and store it in a safe
            place. You won't be able to see it again.
          </h3>
          <p className="text-xs text-amber-800">
            Make sure to copy this key now. You will not be able to retrieve it
            later. Copy it and save it on the device firmware. If you lose it,
            you will need to reset the device and generate a new key.
          </p>
        </div>
      </div>

      <div className="flex items-center gap-2">
        <code className="flex-1 rounded-md bg-white border border-amber-200 px-3 py-2">
          {appKey}
        </code>
        <Button
          type="button"
          variant="outline"
          size="sm"
          onClick={handleCopy}
          className="shrink-0"
        >
          {copied ? (
            <Check className="h-4 w-4" />
          ) : (
            <Copy className="h-4 w-4" />
          )}
        </Button>
      </div>

      <div className="flex justify-end">
        <Button type="button" onClick={onDone}>
          I have copied the App Key, continue.
        </Button>
      </div>
    </div>
  );
}
