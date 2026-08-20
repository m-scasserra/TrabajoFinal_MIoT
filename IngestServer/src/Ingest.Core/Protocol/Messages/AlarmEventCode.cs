namespace Ingest.Core.Protocol.Messages;

public enum AlarmEventCode : byte
{
    Overvoltage = 0x01,
    Overcurrent = 0x02,
    PowerOutage = 0x03,
    PowerRestored = 0x04,
    Unknown = 0xFF
}

public static class AlarmEventCodeExtensions
{
    public static AlarmEventCode FromByte(byte value) =>
        Enum.IsDefined(typeof(AlarmEventCode), value) && value != (byte)AlarmEventCode.Unknown
            ? (AlarmEventCode)value
            : AlarmEventCode.Unknown;
}