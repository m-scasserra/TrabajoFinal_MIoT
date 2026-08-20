namespace Ingest.Core.Protocol.Messages;

public enum ResultCode : byte
{
    Ok = 0x00,
    InvalidRegister = 0x01,
    TableFull = 0x02,
    UnknownCommand = 0x03,
    CommitHashMismatch = 0x04,
    ParamOutOfRange = 0x05,
    InvalidState = 0x06,
    Unknown = 0xFF
}

public static class ResultCodeExtensions
{
    public static ResultCode FromByte(byte value) =>
        Enum.IsDefined(typeof(ResultCode), value) && value != (byte)ResultCode.Unknown
            ? (ResultCode)value
            : ResultCode.Unknown;
}
