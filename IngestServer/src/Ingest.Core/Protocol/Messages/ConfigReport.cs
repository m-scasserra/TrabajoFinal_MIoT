namespace Ingest.Core.Protocol.Messages;

public readonly record struct ConfigReport(
    byte ConfigVersion,
    ushort ConfigHash,
    FragInfo Fragment,
    byte Mode,
    byte PhaseConfig,
    byte SlaveId,
    byte BaudrateCode,
    byte Parity,
    byte RegCount,
    byte RegsInFrag,
    IReadOnlyList<RegisterEntry> Registers)
{
    public static ConfigReport Decode(ref PayloadReader reader)
    {
        byte configVersion = reader.ReadU8();
        ushort configHash = reader.ReadU16();
        FragInfo fragInfo = FragInfo.FromByte(reader.ReadU8());
        byte mode = reader.ReadU8();
        byte phaseConfig = reader.ReadU8();
        byte slaveId = reader.ReadU8();

        byte busParams = reader.ReadU8();
        byte baudrateCode = (byte)(busParams >> 4);
        byte parity = (byte)(busParams & 0x0F);

        byte regCount = reader.ReadU8();
        byte regsInFrag = reader.ReadU8();

        var registers = new RegisterEntry[regsInFrag];
        for (int i = 0; i < regsInFrag; i++)
        {
            registers[i] = RegisterEntry.Decode(ref reader);
        }

        return new ConfigReport(
            configVersion, configHash, fragInfo, mode, phaseConfig,
            slaveId, baudrateCode, parity, regCount, regsInFrag, registers);
    }
}