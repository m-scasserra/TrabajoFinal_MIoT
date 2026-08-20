namespace Ingest.Core.Protocol.Messages;

public readonly record struct MeasurementHeader(
    byte ConfigVersion,
    MeasurementFlags Flags,
    ushort SeqNumber)
{
    public static MeasurementHeader Decode(ref PayloadReader reader)
    {
        byte configVersion = reader.ReadU8();
        MeasurementFlags flags = MeasurementFlags.FromByte(reader.ReadU8());
        ushort seqNumber = reader.ReadU16();

        return new MeasurementHeader(configVersion, flags, seqNumber);
    }
}
