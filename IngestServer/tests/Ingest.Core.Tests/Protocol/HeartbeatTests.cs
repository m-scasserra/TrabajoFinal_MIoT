using Ingest.Core.Protocol;
using Ingest.Core.Protocol.Messages;

namespace Ingest.Core.Tests.Protocol;

public class HeartbeatTests
{
    [Fact]
    public void Vector_DecodeAllFields()
    {
        //   00 04 23 57 01 02 03
        //   │  │  │  │  └──┴─ boot_count = 0x0102 = 258 (u16)
        //   │  │  │  └─────── battery = 0x57 = 87
        //   │  │  └────────── fw_version = 0x23 → mayor 2, menor 3
        //   │  └───────────── config_version = 4
        //   └──────────────── header 0x00 → v0, Heartbeat
        //   flags (último byte) = 0x03 → mains + backlog
        byte[] payload = [0x00, 0x04, 0x23, 0x57, 0x01, 0x02, 0x03];

        var reader = new PayloadReader(payload);
        var header = Header.Parse(ref reader);
        var hb = Heartbeat.Decode(ref reader);

        Assert.Equal(MessageType.Heartbeat, header.Type);
        Assert.Equal(4, hb.ConfigVersion);
        Assert.Equal(2, hb.FwMajor);
        Assert.Equal(3, hb.FwMinor);
        Assert.Equal(87, hb.BatteryPercent);
        Assert.Equal(258, hb.BootCount);
        Assert.True(hb.MainsPresent);
        Assert.True(hb.BacklogNotEmpty);
        Assert.Equal(0x03, hb.RawFlags);
        Assert.Equal(0, reader.Remaining);
    }

    [Theory]
    [InlineData(0x00, false, false)]
    [InlineData(0x01, true, false)]
    [InlineData(0x02, false, true)]
    [InlineData(0x03, true, true)]
    [InlineData(0xFC, false, false)]
    [InlineData(0xFF, true, true)]
    public void Decode_InterpretFlagsBitToBit(byte flags, bool mains, bool backlog)
    {
        byte[] payload = [0x00, 0x04, 0x23, 0x57, 0x01, 0x02, flags];

        var reader = new PayloadReader(payload);
        var header = Header.Parse(ref reader);
        var hb = Heartbeat.Decode(ref reader);

        Assert.Equal(mains, hb.MainsPresent);
        Assert.Equal(backlog, hb.BacklogNotEmpty);
        Assert.Equal(flags, hb.RawFlags);
    }

    [Theory]
    [InlineData(0x10, 1, 0)]
    [InlineData(0x23, 2, 3)]
    [InlineData(0xAF, 10, 15)]
    public void Decode_FwVersionToNibbles(byte fw, byte major, byte minor)
    {
        byte[] payload = [0x00, 0x04, fw, 0x57, 0x01, 0x02, 0x03];

        var reader = new PayloadReader(payload);
        var header = Header.Parse(ref reader);
        var hb = Heartbeat.Decode(ref reader);

        Assert.Equal(major, hb.FwMajor);
        Assert.Equal(minor, hb.FwMinor);
    }
}