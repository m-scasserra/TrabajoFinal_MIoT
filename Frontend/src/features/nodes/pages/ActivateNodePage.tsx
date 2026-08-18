import { useState, type FormEvent } from "react";
import { useNavigate } from "react-router";
import { useActivateNode } from "../hooks/useActivateNode";
import { useDeviceProfiles } from "@/features/deviceProfiles/hooks/useDeviceProfiles";
import { METER_TYPES, meterTypeLabels } from "../constants";
import type { MeterType } from "@/api/endpoints/nodes";
import { useSetTopBar } from "@/components/layout/TopBarContext";
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

export function ActivateNodePage() {
  const navigate = useNavigate();
  const { activate, activating, error } = useActivateNode();
  const {
    profiles,
    loading: loadingProfiles,
    error: profilesError,
  } = useDeviceProfiles();

  useSetTopBar("Activate device");

  const [macAddress, setMacAddress] = useState("");
  const [deviceProfileId, setDeviceProfileId] = useState("");
  const [alias, setAlias] = useState("");
  const [meterType, setMeterType] = useState<MeterType | "">("");
  const [freqMinutes, setFreqMinutes] = useState("");
  const [latitude, setLatitude] = useState("");
  const [longitude, setLongitude] = useState("");

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault();
    if (!meterType) return;

    const result = await activate({
      macAddress: macAddress.trim().toLowerCase(),
      deviceProfileId,
      alias,
      meterType,
      freqMinutes: freqMinutes.trim() === "" ? null : Number(freqMinutes),
      latitude: latitude.trim() === "" ? null : Number(latitude),
      longitude: longitude.trim() === "" ? null : Number(longitude),
    });
    if (result) navigate("/nodes");
  };

  return (
    <div className="max-w-xl">
      <p className="text-sm text-muted-foreground mb-5">
        Activate a new device by providing its MAC address and selecting a
        device profile.
      </p>

      <form onSubmit={handleSubmit} className="space-y-4" noValidate>
        <FormField
          id="macAddress"
          label="MAC Address"
          required
          value={macAddress}
          onChange={(e) => setMacAddress(e.target.value.trim())}
          disabled={activating}
          placeholder="12 hexadecimal characters, e.g. 0123456789AB"
          pattern="^[0-9a-fA-F]{12}"
          maxLength={12}
        />

        <div className="space-y-1.5">
          <Label htmlFor="deviceProfileId">Device Profile</Label>
          <Select
            value={deviceProfileId}
            onValueChange={(v) => setDeviceProfileId(v ?? "")}
            disabled={activating || loadingProfiles}
          >
            <SelectTrigger id="deviceProfileId" className="w-full">
              <SelectValue
                placeholder={
                  loadingProfiles ? "Loading..." : "Select a device profile"
                }
              ></SelectValue>
            </SelectTrigger>
            <SelectContent>
              {profiles.map((p) => (
                <SelectItem key={p.id} value={p.id}>
                  <span className="flex items-center gap-2">
                    {p.name}{" "}
                    <span className="text-muted-foreground text-xs">
                      ({p.region})
                    </span>
                  </span>
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
          {profilesError && (
            <p className="text-sm text-destructive">{profilesError}</p>
          )}
        </div>

        <FormField
          id="alias"
          label="Alias"
          required
          maxLength={100}
          value={alias}
          onChange={(e) => setAlias(e.target.value)}
          disabled={activating}
        />

        <div className="space-y-1.5">
          <Label htmlFor="meterType">Meter Type</Label>
          <Select
            value={meterType}
            onValueChange={(v) => setMeterType(v as MeterType)}
            disabled={activating}
          >
            <SelectTrigger id="meterType" className="w-full">
              <SelectValue placeholder="Select a meter type"></SelectValue>
            </SelectTrigger>
            <SelectContent>
              {METER_TYPES.map((t) => (
                <SelectItem key={t} value={t}>
                  {meterTypeLabels[t]}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
        </div>

        <FormField
          id="freqMinutes"
          label="Frequency (Minutes)"
          optional
          type="number"
          min={1}
          value={freqMinutes}
          onChange={(e) => setFreqMinutes(e.target.value)}
          disabled={activating}
        />

        <div className="grid grid-cols-2 gap-4">
          <FormField
            id="latitude"
            label="Latitude"
            optional
            type="number"
            step="any"
            value={latitude}
            onChange={(e) => setLatitude(e.target.value)}
            disabled={activating}
          />
          <FormField
            id="longitude"
            label="Longitude"
            optional
            type="number"
            step="any"
            value={longitude}
            onChange={(e) => setLongitude(e.target.value)}
            disabled={activating}
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

        <div className="flex items-center gap-2">
          <Button
            type="submit"
            disabled={activating || !meterType || !deviceProfileId}
          >
            {activating ? "Activating..." : "Activate"}
          </Button>
          <Button
            type="button"
            variant="outline"
            onClick={() => navigate("/nodes/")}
            disabled={activating}
          >
            Cancel
          </Button>
        </div>
      </form>
    </div>
  );
}
