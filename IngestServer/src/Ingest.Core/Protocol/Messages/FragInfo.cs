namespace Ingest.Core.Protocol.Messages;

public readonly record struct FragInfo(byte Index, byte Total)
{
    public static FragInfo FromByte(byte value)
    {
        byte index = (byte)(value >> 4);
        byte total = (byte)(value & 0x0F);

        if (total == 0)
        {
            throw new PayloadFormatException($"Invalid frag_info byte: {value:X2}. Total fragments cannot be zero.");
        }

        if (index >= total)
        {
            throw new PayloadFormatException($"Invalid frag_info byte: {value:X2}. Index ({index}) must be less than total ({total}).");
        }
        return new FragInfo(index, total);
    }

    public bool IsSingleFragment => Total == 1;
    public bool IsLast => Index == Total - 1;
}