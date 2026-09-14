using Ingest.Core.Decoding;
using Ingest.Core.Protocol;
using Microsoft.VisualStudio.TestPlatform.Common;

namespace Ingest.Core.Tests.Decoding;

public class RegisterBytesTests
{
    private static byte[] ReadCanonical(byte[] payload, int byteWidth, bool wordSwap)
    {
        var reader = new PayloadReader(payload);
        Span<byte> dest = new byte[byteWidth];
        RegisterBytes.ReadCanonical(ref reader, byteWidth, wordSwap, dest);
        return dest.ToArray();
    }

    [Fact]
    public void U16_NoSwap_CopiesCorrectly()
    {
        byte[] result = ReadCanonical([0x11, 0x22], byteWidth: 2, wordSwap: false);
        Assert.Equal(new byte[] { 0x11, 0x22 }, result);
    }

    [Fact]
    public void U16_WordSwap_NoOp_OneWord()
    {
        byte[] result = ReadCanonical([0x11, 0x22], byteWidth: 2, wordSwap: true);
        Assert.Equal(new byte[] { 0x11, 0x22 }, result);
    }

    [Fact]
    public void U32_NoSwap_HighWordFirst()
    {
        byte[] result = ReadCanonical([0x11, 0x22, 0x33, 0x44], byteWidth: 4, wordSwap: false);
        Assert.Equal(new byte[] { 0x11, 0x22, 0x33, 0x44 }, result);
    }

    [Fact]
    public void U32_WordSwap_SwapsWords()
    {
        byte[] result = ReadCanonical([0x11, 0x22, 0x33, 0x44], byteWidth: 4, wordSwap: true);
        Assert.Equal(new byte[] { 0x33, 0x44, 0x11, 0x22 }, result);
    }

    [Fact]
    public void U64_NoSwap_FourWordsInOrder()
    {
        byte[] input = [0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77, 0x88];
        byte[] result = ReadCanonical(input, byteWidth: 8, wordSwap: false);
        Assert.Equal(input, result);
    }

    [Fact]
    public void U64_WordSwap_SwapsWords()
    {
        byte[] input = [0x77, 0x88, 0x55, 0x66, 0x33, 0x44, 0x11, 0x22];
        byte[] result = ReadCanonical(input, byteWidth: 8, wordSwap: true);
        Assert.Equal(new byte[] { 0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77, 0x88 }, result);
    }

    [Fact]
    public void CursorAdvancesByByteWidth()
    {
        byte[] payload = [0x11, 0x22, 0x33, 0x44, 0xAA, 0xBB];
        var reader = new PayloadReader(payload);
        Span<byte> dest = new byte[4];
        RegisterBytes.ReadCanonical(ref reader, byteWidth: 4, wordSwap: false, dest);

        Assert.Equal(2, reader.Remaining);
        Assert.Equal(0xAA, reader.ReadU8());
    }

    [Fact]
    public void InvalidByteWidth_Throws()
    {
        static void Act()
        {
            var reader = new PayloadReader([0x00, 0x00, 0x00]);
            Span<byte> dest = new byte[3];
            RegisterBytes.ReadCanonical(ref reader, byteWidth: 4, wordSwap: false, dest);
        }

        Assert.Throws<PayloadFormatException>(Act);
    }

    [Fact]
    public void TruncatedPayload_Throws()
    {
        static void Act()
        {
            var reader = new PayloadReader([0x11, 0x22]);
            Span<byte> dest = new byte[4];
            RegisterBytes.ReadCanonical(ref reader, byteWidth: 4, wordSwap: false, dest);
        }

        Assert.Throws<PayloadFormatException>(Act);
    }
}