export const MAC_VERSIONS = [
  "LORAWAN_1_0_0",
  "LORAWAN_1_0_1",
  "LORAWAN_1_0_2",
  "LORAWAN_1_0_3",
  "LORAWAN_1_0_4",
  "LORAWAN_1_1_0",
];

export const REGIONS = ["AU915", "EU868", "US915", "AS923"];

export const REG_PARAMS_REVISIONS = [
  "A",
  "RP002_1_0_0",
  "RP002_1_0_1",
  "RP002_1_0_3",
  "RP002_1_0_4",
];

export const DEFAULT_PROFILE_FORM = {
  name: "",
  region: "AU915",
  macVersion: "LORAWAN_1_0_4",
  regParamsRevision: "RP002_1_0_4",
  regionConfigId: "",
  adrAlgorithmId: "default",
  uplinkInterval: 3600,
  deviceStatusReqInterval: 1,
  supportsOtaa: true,
  flushQueueOnActivate: true,
  autoDetectMeasurements: true,
  ts003FPort: "" as number | "",
  ts004FPort: "" as number | "",
  ts005FPort: "" as number | "",
};
