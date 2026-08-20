namespace Ingest.Core.Protocol.Messages;

public readonly record struct Heartbeat(
    byte ConfigVersion,
    byte FwMajor,
    byte FwMinor,
    byte BatteryPercent,
    ushort BootCount,
    bool MainsPresent,
    bool BacklogNotEmpty,
    byte RawFlags)
{
    public static Heartbeat Decode(ref PayloadReader reader)
    {
        byte configVersion = reader.ReadU8();

        byte fw = reader.ReadU8();
        byte fwMajor = (byte)(fw >> 4);
        byte fwMinor = (byte)(fw & 0x0F);

        byte battery = reader.ReadU8();
        ushort bootCount = reader.ReadU16();
        byte flags = reader.ReadU8();
        bool mainsPresent = (flags & 0b0000_0001) != 0;
        bool backlogNotEmpty = (flags & 0b0000_0010) != 0;

        return new Heartbeat(
            configVersion, fwMajor, fwMinor, battery, bootCount,
            mainsPresent, backlogNotEmpty, flags);
    }
}
