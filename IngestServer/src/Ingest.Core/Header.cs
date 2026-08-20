namespace Ingest.Core.Protocol;

public readonly record struct Header(byte Version, MessageType Type)
{
    public const byte CurrentVersion = 1;

    public static Header Parse(byte headerByte)
    {
        byte version = (byte)(headerByte >> 4);
        byte rawType = (byte)(headerByte & 0x0F);

        if (!Enum.IsDefined(typeof(MessageType), rawType))
        {
            throw new PayloadFormatException($"Invalid message type: 0x{rawType:X1} (header byte: 0x{headerByte:X2}).");
        }

        return new Header(version, (MessageType)rawType);
    }

    public static Header Parse(ref PayloadReader reader) => Parse(reader.ReadU8());
}