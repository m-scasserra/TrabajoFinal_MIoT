namespace Ingest.Core.Protocol.Messages;

public readonly record struct RegisterEntry(
    ushort RegAddr,
    byte RegType,
    byte Scale,
    byte FnCode,
    byte PhaseTag)
{
    public const int SizeBytes = 6;

    public static RegisterEntry Decode(ref PayloadReader reader)
    {
        ushort regAddr = reader.ReadU16();
        byte regType = reader.ReadU8();
        byte scale = reader.ReadU8();
        byte fnCode = reader.ReadU8();
        byte phaseTag = reader.ReadU8();

        return new RegisterEntry(regAddr, regType, scale, fnCode, phaseTag);
    }
}