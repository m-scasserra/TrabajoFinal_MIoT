namespace Ingest.Core.Protocol;

public enum PhaseTag : byte
{
    General = 0x00,
    L1 = 0x01,
    L2 = 0x02,
    L3 = 0x03,
    Neutral = 0x04,
    Unknown = 0xFF
}

public static class PhaseTagExtensions
{
    public static PhaseTag FromByte(byte value) =>
        Enum.IsDefined(typeof(PhaseTag), value) && value != (byte)PhaseTag.Unknown
            ? (PhaseTag)value
            : PhaseTag.Unknown;
}