using Ingest.Core.Protocol;

namespace Ingest.Core.Tests.Protocol;

public class HeaderTests
{
    [Fact]
    public void Parse_ConfigAckHeader_ExtractsVersionAndType()
    {
        var header = Header.Parse(0x14);

        Assert.Equal(1, header.Version);
        Assert.Equal(MessageType.ConfigAck, header.Type);
    }

    [Theory]
    [InlineData(0x11, 1, MessageType.Measurement)]
    [InlineData(0x00, 0, MessageType.Heartbeat)]
    [InlineData(0x13, 1, MessageType.Alarm)]
    [InlineData(0x1B, 1, MessageType.CmdControl)]
    public void Parse_ValidHeader_ExtractsVersionAndType(
        byte headerByte, byte expectedVersion, MessageType expectedType)
    {
        var header = Header.Parse(headerByte);

        Assert.Equal(expectedVersion, header.Version);
        Assert.Equal(expectedType, header.Type);
    }

    [Theory]
    [InlineData(0x16)]
    [InlineData(0x17)]
    [InlineData(0x1C)]
    [InlineData(0x1F)]
    public void Parse_InvalidHeader_Throws(byte headerByte)
    {
        Assert.Throws<PayloadFormatException>(() => Header.Parse(headerByte));
    }

    [Fact]
    public void Parse_NoValidation_AcceptsAnyType()
    {
        var header = Header.Parse(0x24);

        Assert.Equal(2, header.Version);
        Assert.Equal(MessageType.ConfigAck, header.Type);
    }
}