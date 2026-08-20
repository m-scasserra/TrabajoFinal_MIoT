using Ingest.Core.Protocol;
using Ingest.Core.Protocol.Messages;

namespace Ingest.Core.Tests.Protocol;

public class AlarmTests
{
    [Fact]
    public void Vector_OverVoltage_L2_Decode()
    {
        //   13 04 01 2C 01 02 08 FC
        //   │  │  │──┤  │  │  └──┴─ value = 0x08FC = 2300
        //   │  │  │  │  │  └─────── phase = 0x02 (L2)
        //   │  │  │  │  └────────── event_code = 0x01 (sobretensión)
        //   │  │  └──┴───────────── seq_number = 0x012C = 300 (u16)
        //   │  └─────────────────── config_version = 4
        //   └────────────────────── header 0x13 → v1, Alarm
        byte[] payload = [0x13, 0x04, 0x01, 0x2C, 0x01, 0x02, 0x08, 0xFC];

        var reader = new PayloadReader(payload);
        var header = Header.Parse(ref reader);
        var alarm = Alarm.Decode(ref reader);

        Assert.Equal(MessageType.Alarm, header.Type);
        Assert.Equal(4, alarm.ConfigVersion);
        Assert.Equal(300, alarm.SeqNumber);
        Assert.Equal(AlarmEventCode.Overvoltage, alarm.Event);
        Assert.Equal(0x01, alarm.RawEventCode);
        Assert.Equal(PhaseTag.L2, alarm.Phase);
        Assert.Equal(0x02, alarm.RawPhase);
        Assert.Equal(2300, alarm.Value);
        Assert.Equal(0, reader.Remaining);
    }

    [Theory]
    [InlineData(0x01, AlarmEventCode.Overvoltage)]
    [InlineData(0x02, AlarmEventCode.Overcurrent)]
    [InlineData(0x03, AlarmEventCode.PowerOutage)]
    [InlineData(0x04, AlarmEventCode.PowerRestored)]
    public void Decode_MapsEventCodes(byte raw, AlarmEventCode expected)
    {
        byte[] payload = [0x13, 0x04, 0x00, 0x00, raw, 0x00, 0x00, 0x00];

        var reader = new PayloadReader(payload);
        var header = Header.Parse(ref reader);
        var alarm = Alarm.Decode(ref reader);

        Assert.Equal(expected, alarm.Event);
        Assert.Equal(raw, alarm.RawEventCode);
    }

    [Theory]
    [InlineData(0x00, PhaseTag.General)]
    [InlineData(0x01, PhaseTag.L1)]
    [InlineData(0x02, PhaseTag.L2)]
    [InlineData(0x03, PhaseTag.L3)]
    [InlineData(0x04, PhaseTag.Neutral)]
    public void Decode_MapsPhaseTags(byte raw, PhaseTag expected)
    {
        byte[] payload = [0x13, 0x04, 0x00, 0x00, 0x00, raw, 0x00, 0x00];

        var reader = new PayloadReader(payload);
        var header = Header.Parse(ref reader);
        var alarm = Alarm.Decode(ref reader);

        Assert.Equal(expected, alarm.Phase);
        Assert.Equal(raw, alarm.RawPhase);
    }

    [Fact]
    public void Decode_UnknownEventCode_MapsUnknownPreservRaw()
    {
        byte[] payload = [0x13, 0x04, 0x00, 0x00, 0x7E, 0x00, 0x00, 0x00];

        var reader = new PayloadReader(payload);
        var header = Header.Parse(ref reader);
        var alarm = Alarm.Decode(ref reader);

        Assert.Equal(AlarmEventCode.Unknown, alarm.Event);
        Assert.Equal(0x7E, alarm.RawEventCode);
    }

    [Fact]
    public void Decode_UnknownPhaseTag_MapsUnknownPreservRaw()
    {
        byte[] payload = [0x13, 0x04, 0x01, 0x00, 0x00, 0x09, 0x00, 0x00];

        var reader = new PayloadReader(payload);
        var header = Header.Parse(ref reader);
        var alarm = Alarm.Decode(ref reader);

        Assert.Equal(PhaseTag.Unknown, alarm.Phase);
        Assert.Equal(0x09, alarm.RawPhase);
    }
}