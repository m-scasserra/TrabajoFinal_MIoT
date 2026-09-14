using Ingest.Core.Protocol;

namespace Ingest.Core.Decoding;

public static class RegisterBytes
{
    public static void ReadCanonical(
        ref PayloadReader reader, int byteWidth, bool wordSwap, Span<byte> destination)
    {
        if (byteWidth is not (2 or 4 or 8))
        {
            throw new PayloadFormatException(
                $"Invalid width of register: {byteWidth} bytes when only 2, 4, or 8 are allowed."
            );
        }
        if (destination.Length < byteWidth)
        {
            throw new PayloadFormatException(
                $"Destination buffer is too small: {destination.Length} bytes when {byteWidth} bytes are required."
            );
        }

        Span<byte> raw = stackalloc byte[byteWidth];
        for (int i = 0; i < byteWidth; i++)
        {
            raw[i] = reader.ReadU8();
        }

        if (!wordSwap || byteWidth == 2)
        {
            raw[..byteWidth].CopyTo(destination);
            return;
        }

        int wordCount = byteWidth / 2;
        for (int w = 0; w < wordCount; w++)
        {
            int srcWord = wordCount - 1 - w;
            destination[w * 2] = raw[srcWord * 2];
            destination[w * 2 + 1] = raw[srcWord * 2 + 1];
        }
    }
}