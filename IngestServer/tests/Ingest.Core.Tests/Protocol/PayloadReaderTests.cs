using Ingest.Core.Protocol;

namespace Ingest.Core.Tests.Protocol;

public class PayloadReaderTests
{
    [Fact]
    public void Vector_ConfigAck_Example_DecodesAllField()
    {
        // Vector de la spec (sección 10), config_ack tras el COMMIT:
        //   14 0B 00 04 9F 3A
        //   │  │  │  │  └──┴─ config_hash   = 0x9F3A (u16)
        //   │  │  │  └─────── config_version = 4     (u8)
        //   │  │  └────────── result_code    = 0x00  (u8)
        //   │  └───────────── cmd_id         = 0x0B  (u8)
        //   └──────────────── header 0x14 → v1, ConfigAck
        byte[] payload = [0x14, 0x0B, 0x00, 0x04, 0x9F, 0x3A];

        var reader = new PayloadReader(payload);

        var header = Header.Parse(ref reader);
        var cmdId = reader.ReadU8();
        var resultCode = reader.ReadU8();
        var configVersion = reader.ReadU8();
        var configHash = reader.ReadU16();

        Assert.Equal(1, header.Version);
        Assert.Equal(MessageType.ConfigAck, header.Type);
        Assert.Equal(0x0B, cmdId);
        Assert.Equal(0x00, resultCode);
        Assert.Equal(4, configVersion);
        Assert.Equal(0x9F3A, configHash);

        Assert.Equal(0, reader.Remaining);
        Assert.Equal(6, reader.Position);
    }

    [Fact]
    public void Remaining_YPosition_AdvancesEachRead()
    {
        byte[] payload = [0xAA, 0xBB, 0xCC, 0xDD];
        var reader = new PayloadReader(payload);

        Assert.Equal(4, reader.Remaining);
        Assert.Equal(0, reader.Position);

        reader.ReadU8();
        Assert.Equal(3, reader.Remaining);
        Assert.Equal(1, reader.Position);

        reader.ReadU16();
        Assert.Equal(1, reader.Remaining);
        Assert.Equal(3, reader.Position);
    }


    [Fact]
    public void ReadU16_BigEndian()
    {
        byte[] payload = [0x9F, 0x3A];
        var reader = new PayloadReader(payload);

        Assert.Equal(0x9F3A, reader.ReadU16());
    }

    [Fact]
    public void ReadU32_BigEndian()
    {
        byte[] payload = [0x01, 0x02, 0x03, 0x04];
        var reader = new PayloadReader(payload);

        Assert.Equal(0x01020304u, reader.ReadU32());
    }

    [Theory]
    [InlineData(new byte[] { 0xFF, 0xFF }, -1)]
    [InlineData(new byte[] { 0x80, 0x00 }, -32768)]
    [InlineData(new byte[] { 0x7F, 0xFF }, 32767)]
    public void ReadI16_TwoComplement(byte[] payload, short expected)
    {
        var reader = new PayloadReader(payload);

        Assert.Equal(expected, reader.ReadI16());
    }

    [Fact]
    public void ReadI32_TwoComplement()
    {
        byte[] payload = [0xFF, 0xFF, 0xFF, 0xFF];
        var reader = new PayloadReader(payload);

        Assert.Equal(-1, reader.ReadI32());
    }

    [Fact]
    public void ReadU16_OneByteRemaining_Throws()
    {
        byte[] payload = [0x42];
        var reader = new PayloadReader(payload);

        Assert.Throws<PayloadFormatException>(() =>
        {
            var r = new PayloadReader(payload);
            r.ReadU16();
        });
    }

    [Fact]
    public void ReadU8_EmptyBuffer_Throws()
    {
        Assert.Throws<PayloadFormatException>(() =>
        {
            var r = new PayloadReader(ReadOnlySpan<byte>.Empty);
            r.ReadU8();
        });
    }
}