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
}: DeviceProfileFormProps) {}
