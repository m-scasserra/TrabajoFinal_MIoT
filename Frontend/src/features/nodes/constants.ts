import type { MeterType } from "@/api/endpoints/nodes";

export const METER_TYPES: MeterType[] = ["MODBUS_RTU", "PULSE", "ANALOG"];

export const meterTypeLabels: Record<MeterType, string> = {
  MODBUS_RTU: "Modbus RTU",
  PULSE: "Pulse",
  ANALOG: "Analog",
};
