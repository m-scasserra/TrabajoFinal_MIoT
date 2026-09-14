namespace Ingest.Core.Protocol.Messages;

public enum AlarmEventCode : byte
{
    Overvoltage = 0x01,
    Undervoltage = 0x02,
    Overcurrent = 0x03,
    Undercurrent = 0x04,
    Overpower = 0x05,
    Underpower = 0x06,
    PowerOutage = 0x07,
    PowerRestored = 0x08,
    NoCommunication = 0x09,
    Unknown = 0xFF
}

public static class AlarmEventCodeExtensions
{
    public static AlarmEventCode FromByte(byte value) =>
        Enum.IsDefined(typeof(AlarmEventCode), value) && value != (byte)AlarmEventCode.Unknown
            ? (AlarmEventCode)value
            : AlarmEventCode.Unknown;
}