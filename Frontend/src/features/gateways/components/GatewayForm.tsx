import { useState, type FormEvent } from "react";
import { OPERATIVE_STATES, operativeStateLabels } from "../constants";
import type { Gateway } from "@/api/endpoints/gateways";
import type { OperativeState } from "@/api/types";
import { FormField } from "@/components/FormField";
import { Button } from "@/components/ui/button";
import { Label } from "@/components/ui/label";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";

export interface GatewayFormValues {
  gatewayEui: string;
  alias: string;
  model: string;
  operativeState: OperativeState;
  latitude: string;
  longitude: string;
}

function initialValues(gw?: Gateway): GatewayFormValues {
  return {
    gatewayEui: gw?.gatewayEui ?? "",
    alias: gw?.alias ?? "",
    model: gw?.model ?? "",
    operativeState: gw?.operativeState ?? "ACTIVE",
    latitude: gw?.latitude != null ? String(gw.latitude) : "",
    longitude: gw?.longitude != null ? String(gw.longitude) : "",
  };
}

interface GatewayFormProps {
  mode: "create" | "edit";
  initial?: Gateway;
  saving: boolean;
  error: string | null;
  onSubmit: (values: GatewayFormValues) => void;
  onCancel: () => void;
}

export function GatewayForm({
  mode,
  initial,
  saving,
  error,
  onSubmit,
  onCancel,
}: GatewayFormProps) {
  const [values, setValues] = useState<GatewayFormValues>(
    initialValues(initial),
  );
  const set = <K extends keyof GatewayFormValues>(
    k: K,
    v: GatewayFormValues[K],
  ) => setValues((s) => ({ ...s, [k]: v }));

  const handleSubmit = (e: FormEvent) => {
    e.preventDefault();
    onSubmit(values);
  };

  return (
    <form onSubmit={handleSubmit} className="space-y-4" noValidate>
      {mode === "create" ? (
        <FormField
          id="gatewayEui"
          label="Gateway EUI"
          required
          value={values.gatewayEui}
          onChange={(e) => set("gatewayEui", e.target.value.trim())}
          disabled={saving}
          placeholder="Enter Gateway EUI (16 hex characters)"
          pattern="^[0-9a-fA-F]{16}"
          maxLength={16}
        />
      ) : (
        <div className="space-y-1.5">
          <Label>Gateway EUI</Label>
          <p className="font-mono text-sm text-muted-foreground">
            {initial?.gatewayEui}
          </p>
        </div>
      )}

      <FormField
        id="alias"
        label="Alias"
        required
        maxLength={100}
        value={values.alias}
        onChange={(e) => set("alias", e.target.value)}
        disabled={saving}
      />
      <FormField
        id="model"
        label="Model"
        required
        maxLength={100}
        value={values.model}
        onChange={(e) => set("model", e.target.value)}
        disabled={saving}
      />

      {mode === "edit" && (
        <div className="space-y-1.5">
          <Label htmlFor="operativeState">Operative State</Label>
          <Select
            value={values.operativeState}
            onValueChange={(v) => set("operativeState", v as OperativeState)}
            disabled={saving}
          >
            <SelectTrigger id="operativeState" className="w-full">
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              {OPERATIVE_STATES.map((s) => (
                <SelectItem key={s} value={s}>
                  {operativeStateLabels[s]}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
        </div>
      )}

      <div className="grid grid-cols-2 gap-4">
        <FormField
          id="latitude"
          label="Latitude"
          optional
          type="number"
          step="any"
          value={values.latitude}
          onChange={(e) => set("latitude", e.target.value)}
          disabled={saving}
        />
        <FormField
          id="longitude"
          label="Longitude"
          optional
          type="number"
          step="any"
          value={values.longitude}
          onChange={(e) => set("longitude", e.target.value)}
          disabled={saving}
        />
      </div>

      {error && (
        <p
          role="alert"
          className="text-sm text-destructive bg-destructive/10 border border-destructive/20 rounded-md px-3 py-2"
        >
          {error}
        </p>
      )}

      <div className="flex items-center gap-3 pt-1">
        <Button type="submit" disabled={saving}>
          {saving
            ? "Saving..."
            : mode === "create"
              ? "Create Gateway"
              : "Save changes"}
        </Button>
        <Button
          type="button"
          variant="outline"
          onClick={onCancel}
          disabled={saving}
        >
          Cancel
        </Button>
      </div>
    </form>
  );
}

export function parseCoords(values: GatewayFormValues): {
  latitude: number | null;
  longitude: number | null;
} {
  const lat = values.latitude.trim();
  const lng = values.longitude.trim();
  return {
    latitude: lat === "" ? null : Number(lat),
    longitude: lng === "" ? null : Number(lng),
  };
}
