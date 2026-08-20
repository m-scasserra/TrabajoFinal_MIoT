namespace Ingest.Core.Protocol.Messages;

using Ingest.Core.Protocol;
public readonly record struct Alarm(
    byte ConfigVersion,
    ushort SeqNumber,
    AlarmEventCode Event,
    byte RawEventCode,
    PhaseTag Phase,
    byte RawPhase,
    ushort Value)
{
    public static Alarm Decode(ref PayloadReader reader)
    {
        byte configVersion = reader.ReadU8();
        ushort seqNumber = reader.ReadU16();
        byte rawEvent = reader.ReadU8();
        byte rawPhase = reader.ReadU8();
        ushort value = reader.ReadU16();

        return new Alarm(
            configVersion,
            seqNumber,
            AlarmEventCodeExtensions.FromByte(rawEvent),
            rawEvent,
            PhaseTagExtensions.FromByte(rawPhase),
            rawPhase,
            value);
    }
}

