import { useState, type FormEvent } from "react";
import {
  MAC_VERSIONS,
  REGIONS,
  REG_PARAMS_REVISIONS,
  DEFAULT_PROFILE_FORM,
} from "../constants";
import type {
  DeviceProfile,
  DeviceProfilePayload,
} from "@/api/endpoints/deviceProfiles";
import { FormField } from "@/components/FormField";
import { Button } from "@/components/ui/button";
import { Label } from "@/components/ui/label";
import { Switch } from "@/components/ui/switch";
import { Separator } from "@/components/ui/separator";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";

type FormState = typeof DEFAULT_PROFILE_FORM;

function profileToForm(p: DeviceProfile): FormState {
  return {
    name: p.name,
    region: p.region,
    macVersion: p.macVersion,
    regParamsRevision: p.regParamsRevision,
    regionConfigId: p.regionConfigId ?? "",
    adrAlgorithmId: p.adrAlgorithmId ?? "default",
    uplinkInterval: p.uplinkInterval,
    deviceStatusReqInterval: p.deviceStatusReqInterval,
    supportsOtaa: p.supportsOtaa,
    flushQueueOnActivate: p.flushQueueOnActivate,
    autoDetectMeasurements: p.autoDetectMeasurements,
    ts003FPort: p.appLayerParams?.ts003FPort ?? "",
    ts004FPort: p.appLayerParams?.ts004FPort ?? "",
    ts005FPort: p.appLayerParams?.ts005FPort ?? "",
  };
}

function formToPayload(form: FormState): DeviceProfilePayload {
  const hasAppLayer =
    form.ts003FPort !== "" || form.ts004FPort !== "" || form.ts005FPort !== "";
  const appLayerParams = hasAppLayer
    ? {
        ts003FPort: form.ts003FPort === "" ? undefined : form.ts003FPort,
        ts004FPort: form.ts004FPort === "" ? undefined : form.ts004FPort,
        ts005FPort: form.ts005FPort === "" ? undefined : form.ts005FPort,
      }
    : null;

  return {
    name: form.name,
    region: form.region,
    macVersion: form.macVersion,
    regParamsRevision: form.regParamsRevision,
    regionConfigId:
      form.regionConfigId.trim() === "" ? null : form.regionConfigId.trim(),
    adrAlgorithmId:
      form.adrAlgorithmId.trim() === ""
        ? "default"
        : form.adrAlgorithmId.trim(),
    uplinkInterval: Number(form.uplinkInterval),
    deviceStatusReqInterval: Number(form.deviceStatusReqInterval),
    supportsOtaa: form.supportsOtaa,
    flushQueueOnActivate: form.flushQueueOnActivate,
    autoDetectMeasurements: form.autoDetectMeasurements,
    appLayerParams,
  };
}

interface DeviceProfileFormProps {
  initial?: DeviceProfile;
  saving: boolean;
  error: string | null;
  submitLabel: string;
  onSubmit: (payload: DeviceProfilePayload) => void;
  onCancel: () => void;
}

export function DeviceProfileForm({
  initial,
  saving,
  error,
  submitLabel,
  onSubmit,
  onCancel,
}: DeviceProfileFormProps) {
  const [form, setForm] = useState<FormState>(
    initial ? profileToForm(initial) : DEFAULT_PROFILE_FORM,
  );

  const set = <k extends keyof FormState>(key: k, value: FormState[k]) =>
    setForm((f) => ({ ...f, [key]: value }));

  const handleSubmit = (e: FormEvent) => {
    e.preventDefault();
    onSubmit(formToPayload(form));
  };

  return (
    <form onSubmit={handleSubmit} className="space-y-6 noValidate">
      {/* --- Identification --- */}
      <section className="space-y-4">
        <FormField
          id="name"
          label="Name"
          required
          maxLength={100}
          value={form.name}
          onChange={(e) => set("name", e.target.value)}
          disabled={saving}
        />
      </section>

      <Separator />

      {/* --- LoRaWAN Configuration --- */}
      <section className="space-y-4">
        <h3 className="text-sm font-medium text-foreground">
          LoRaWAN Configuration
        </h3>

        <div className="grid grid-cols-2 gap-4">
          <SelectField
            id="region"
            label="Region"
            value={form.region}
            options={REGIONS}
            onChange={(v) => (v ? set("region", v) : set("region", ""))}
            disabled={saving}
          />
          <SelectField
            id="macVersion"
            label="MAC Version"
            value={form.macVersion}
            options={MAC_VERSIONS}
            onChange={(v) => (v ? set("macVersion", v) : set("macVersion", ""))}
            disabled={saving}
          />
          <SelectField
            id="regParamsRevision"
            label="Reg Params Revision"
            value={form.regParamsRevision}
            options={REG_PARAMS_REVISIONS}
            onChange={(v) =>
              v ? set("regParamsRevision", v) : set("regParamsRevision", "")
            }
            disabled={saving}
          />
          <FormField
            id="regionConfigId"
            label="Region Config ID"
            optional
            maxLength={50}
            value={form.regionConfigId}
            onChange={(e) => set("regionConfigId", e.target.value)}
            disabled={saving}
          />
        </div>

        <FormField
          id="adrAlgorithmId"
          label="ADR Algorithm ID"
          optional
          maxLength={50}
          value={form.adrAlgorithmId}
          onChange={(e) => set("adrAlgorithmId", e.target.value)}
          disabled={saving}
        />
      </section>

      <Separator />

      {/* --- Intervals --- */}
      <section className="space-y-4">
        <h3 className="text-sm font-medium text-foreground">Intervals</h3>
        <div className="grid grid-cols-2 gap-4">
          <FormField
            id="uplinkInterval"
            label="Uplink Interval (s)"
            type="number"
            min={1}
            max={86400}
            required
            value={form.uplinkInterval}
            onChange={(e) => set("uplinkInterval", Number(e.target.value))}
            disabled={saving}
          />
          <FormField
            id="deviceStatusReqInterval"
            label="Device Status Req Interval"
            type="number"
            min={1}
            max={100}
            required
            value={form.deviceStatusReqInterval}
            onChange={(e) =>
              set("deviceStatusReqInterval", Number(e.target.value))
            }
            disabled={saving}
          />
        </div>
      </section>

      <Separator />

      {/* --- Options --- */}
      <section className="space-y-3">
        <h3 className="text-sm font-medium text-foreground">Options</h3>
        <ToggleRow
          id="supportsOtaa"
          label="Supports OTAA"
          checked={form.supportsOtaa}
          onChange={(v) => set("supportsOtaa", v)}
          disabled={saving}
        />
        <ToggleRow
          id="flushQueueOnActivate"
          label="Flush Queue On Activate"
          checked={form.flushQueueOnActivate}
          onChange={(v) => set("flushQueueOnActivate", v)}
          disabled={saving}
        />
        <ToggleRow
          id="autoDetectMeasurements"
          label="Auto Detect Measurements"
          checked={form.autoDetectMeasurements}
          onChange={(v) => set("autoDetectMeasurements", v)}
          disabled={saving}
        />
      </section>

      <Separator />

      {/* --- App Layer --- */}
      <section className="space-y-4">
        <div>
          <h3 className="text-sm font-medium text-foreground">
            App Layer FPorts
          </h3>
          <p className="text-xs text-muted-foreground mt-0.5">
            Optional FPorts for application layer measurements. Leave blank if
            not used.
          </p>
        </div>
        <div className="grid grid-cols-3 gap-4">
          <FormField
            id="ts003FPort"
            label="TS003 FPort"
            type="number"
            optional
            value={form.ts003FPort}
            onChange={(e) =>
              set(
                "ts003FPort",
                e.target.value === "" ? "" : Number(e.target.value),
              )
            }
            disabled={saving}
          />
          <FormField
            id="ts004FPort"
            label="TS004 FPort"
            type="number"
            optional
            value={form.ts004FPort}
            onChange={(e) =>
              set(
                "ts004FPort",
                e.target.value === "" ? "" : Number(e.target.value),
              )
            }
            disabled={saving}
          />
          <FormField
            id="ts005FPort"
            label="TS005 FPort"
            type="number"
            optional
            value={form.ts005FPort}
            onChange={(e) =>
              set(
                "ts005FPort",
                e.target.value === "" ? "" : Number(e.target.value),
              )
            }
            disabled={saving}
          />
        </div>
      </section>

      {error && (
        <p
          role="alert"
          className="text-sm text-destructive bg-destructive/10 border border-destructive/20 rounded-md px-3 py-2"
        >
          {error}
        </p>
      )}

      <div className="flex items-center gap-3">
        <Button type="submit" disabled={saving}>
          {saving ? "Saving..." : submitLabel}
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

function SelectField({
  id,
  label,
  value,
  options,
  onChange,
  disabled,
}: {
  id: string;
  label: string;
  value: string;
  options: readonly string[];
  onChange: (v: string | null) => void;
  disabled?: boolean;
}) {
  return (
    <div className="space-y-1.5">
      <Label htmlFor={id}>{label}</Label>
      <Select value={value} onValueChange={onChange} disabled={disabled}>
        <SelectTrigger id={id} className="w-full">
          <SelectValue />
        </SelectTrigger>
        <SelectContent>
          {options.map((o) => (
            <SelectItem key={o} value={o}>
              {o}
            </SelectItem>
          ))}
        </SelectContent>
      </Select>
    </div>
  );
}

function ToggleRow({
  id,
  label,
  checked,
  onChange,
  disabled,
}: {
  id: string;
  label: string;
  checked: boolean;
  onChange: (v: boolean) => void;
  disabled?: boolean;
}) {
  return (
    <div className="flex items-center justify-between rounded-lg border p-3">
      <Label htmlFor={id} className="cursor-pointer">
        {label}
      </Label>
      <Switch
        id={id}
        checked={checked}
        onCheckedChange={onChange}
        disabled={disabled}
      />
    </div>
  );
}
