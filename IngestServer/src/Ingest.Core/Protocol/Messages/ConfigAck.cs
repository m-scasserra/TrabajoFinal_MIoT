namespace Ingest.Core.Protocol.Messages;

public readonly record struct ConfigAck(
    byte CmdId,
    ResultCode Result,
    byte RawResultCode,
    byte ConfigVersion,
    ushort ConfigHash)
{
    public static ConfigAck Decode(ref PayloadReader reader)
    {
        byte cmdId = reader.ReadU8();
        byte rawResult = reader.ReadU8();
        byte configVersion = reader.ReadU8();
        ushort configHash = reader.ReadU16();

        return new ConfigAck(
            cmdId, ResultCodeExtensions.FromByte(rawResult), rawResult, configVersion, configHash);
    }
}
