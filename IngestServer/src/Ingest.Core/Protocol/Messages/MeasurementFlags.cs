namespace Ingest.Core.Protocol.Messages;

public readonly record struct MeasurementFlags(byte Raw)
{
    public bool IsBacklog => (Raw & 0b0000_0001) != 0;

    public bool BatteryLow => (Raw & 0b0000_0010) != 0;
    public static MeasurementFlags FromByte(byte value) => new(value);
}
