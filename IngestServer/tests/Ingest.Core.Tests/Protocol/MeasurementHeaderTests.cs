using Ingest.Core.Protocol;
using Ingest.Core.Protocol.Messages;

namespace Ingest.Core.Tests.Protocol;

public class MeasurementHeaderTests
{
    [Fact]
    public void Vector_DecodeHeaderLiveAndLeavesCursorOnPayload()
    {
        // measurement de la sección 10 (trifásico Modbus):
        //   11 04 00 00 01 0F <V_L1> <V_L2> <V_L3>
        //   │  │  │  └──┴─ seq_number = 1
        //   │  │  └─────── flags = 0x00 (live)
        //   │  └────────── config_version = 4
        //   └───────────── header 0x11 → v1, Measurement
        // luego: 0F = phase_mask, seguido de los valores (aquí de relleno)
        byte[] payload = [0x11, 0x04, 0x00, 0x00, 0x01, 0x0F, 0xAA, 0xBB];

        var reader = new PayloadReader(payload);
        var header = Header.Parse(ref reader);
        var mh = MeasurementHeader.Decode(ref reader);

        Assert.Equal(MessageType.Measurement, header.Type);
        Assert.Equal(4, mh.ConfigVersion);
        Assert.False(mh.Flags.IsBacklog);
        Assert.False(mh.Flags.BatteryLow);
        Assert.Equal(1, mh.SeqNumber);

        Assert.Equal(5, reader.Position);
        var mask = PhaseMask.FromByte(reader.ReadU8());
        Assert.Equal(0x0F, mask.Raw);
        Assert.True(mask.HasL1);
        Assert.True(mask.HasL2);
        Assert.True(mask.HasL3);
        Assert.Equal(3, mask.ActivePhaseCount);
    }

    [Fact]
    public void Decode_Backlog_MarkFlag()
    {
        byte[] payload = [0x12, 0x04, 0x01, 0x12, 0x34, 0x02];

        var reader = new PayloadReader(payload);
        var header = Header.Parse(ref reader);
        var mh = MeasurementHeader.Decode(ref reader);

        Assert.Equal(MessageType.MeasurementBacklog, header.Type);
        Assert.True(mh.Flags.IsBacklog);
        Assert.Equal(0x1234, mh.SeqNumber);
    }

    [Fact]
    public void Decode_BattLowFlag()
    {
        byte[] payload = [0x11, 0x04, 0x02, 0x00, 0x05, 0x02];

        var reader = new PayloadReader(payload);
        var header = Header.Parse(ref reader);
        var mh = MeasurementHeader.Decode(ref reader);

        Assert.False(mh.Flags.IsBacklog);
        Assert.True(mh.Flags.BatteryLow);
    }
}