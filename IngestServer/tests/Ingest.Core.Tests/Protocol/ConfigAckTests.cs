using Ingest.Core.Protocol;
using Ingest.Core.Protocol.Messages;

namespace Ingest.Core.Tests.Protocol;

public class ConfigAckTests
{
    [Fact]
    public void Vector_Example_DecodeAndMapsResult()
    {
        byte[] payload = [0x14, 0x0B, 0x00, 0x04, 0x9F, 0x3A];

        var reader = new PayloadReader(payload);
        var header = Header.Parse(ref reader);
        var ack = ConfigAck.Decode(ref reader);

        Assert.Equal(MessageType.ConfigAck, header.Type);
        Assert.Equal(0x0B, ack.CmdId);
        Assert.Equal(ResultCode.Ok, ack.Result);
        Assert.Equal(0x00, ack.RawResultCode);
        Assert.Equal(4, ack.ConfigVersion);
        Assert.Equal(0x9F3A, ack.ConfigHash);
        Assert.Equal(0, reader.Remaining);
    }

    [Theory]
    [InlineData(0x00, ResultCode.Ok)]
    [InlineData(0x01, ResultCode.InvalidRegister)]
    [InlineData(0x02, ResultCode.TableFull)]
    [InlineData(0x03, ResultCode.UnknownCommand)]
    [InlineData(0x04, ResultCode.CommitHashMismatch)]
    [InlineData(0x05, ResultCode.ParamOutOfRange)]
    [InlineData(0x06, ResultCode.InvalidState)]
    public void Decode_MapsAllResultsFromTable(byte raw, ResultCode expected)
    {
        byte[] payload = [0x14, 0x01, raw, 0x04, 0x00, 0x00];

        var reader = new PayloadReader(payload);
        var header = Header.Parse(ref reader);
        var ack = ConfigAck.Decode(ref reader);

        Assert.Equal(expected, ack.Result);
        Assert.Equal(raw, ack.RawResultCode);
    }

    [Theory]
    [InlineData(0x07)]
    [InlineData(0x42)]
    [InlineData(0xFE)]
    public void Decode_MapsUnknownResultsToUnknown_ConservesRaw(byte raw)
    {
        byte[] payload = [0x14, 0x01, raw, 0x04, 0x00, 0x00];

        var reader = new PayloadReader(payload);
        var header = Header.Parse(ref reader);
        var ack = ConfigAck.Decode(ref reader);

        Assert.Equal(ResultCode.Unknown, ack.Result);
        Assert.Equal(raw, ack.RawResultCode);
    }
}