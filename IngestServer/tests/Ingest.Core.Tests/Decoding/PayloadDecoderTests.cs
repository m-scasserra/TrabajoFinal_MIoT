using Ingest.Core.Config;
using Ingest.Core.Decoding;
using Ingest.Core.Protocol;

namespace Ingest.Core.Tests.Decoding;

public class PayloadDecoderTests
{
    private static NodeConfig PulseConfigL1() => new()
    {
        Schema = 1,
        Mode = AcquisitionMode.Pulse,
        ConfigVersion = 7,
        PhaseConfig = new PhaseConfig
        {
            RawByte = 0x02,
            ThreePhase = false,
            PhaseBits = 1,
            ActivePhases = [PhaseTag.L1],
        },
        PulseCounters = [new PulseCounter { Phase = PhaseTag.L1, PulsesPerKwh = 1000 }],
    };

    [Fact]
    public void Alarm_ParsesCorrectlyWithoutConfig()
    {
        byte[] payload = [0x13, 0x04, 0x01, 0x2C, 0x01, 0x02, 0x08, 0xFC];

        var result = PayloadDecoder.Decode(payload, config: null);

        Assert.True(result.IsSuccess);
        var msg = Assert.IsType<DecodedMessage.AlarmMessage>(result.Message);
        Assert.Equal(Ingest.Core.Protocol.Messages.AlarmEventCode.Overvoltage, msg.Alarm.Event);
        Assert.Equal(PhaseTag.L2, msg.Alarm.Phase);
        Assert.Equal(1, msg.ProtocolVersion);
    }

    [Fact]
    public void Heartbeat_ParsesCorrectly()
    {
        byte[] payload = [0x00, 0x04, 0x23, 0x57, 0x01, 0x02, 0x08, 0x03];

        var result = PayloadDecoder.Decode(payload, config: null);

        Assert.True(result.IsSuccess);
        var msg = Assert.IsType<DecodedMessage.HeartbeatMessage>(result.Message);
        Assert.Equal(87, msg.Heartbeat.BatteryPercent);
    }

    [Fact]
    public void ConfigAck_ParsesCorrectly()
    {
        byte[] payload = [0x14, 0x0B, 0x00, 0x04, 0x9F, 0x3A];

        var result = PayloadDecoder.Decode(payload, config: null);

        Assert.True(result.IsSuccess);
        var msg = Assert.IsType<DecodedMessage.ConfigAckMessage>(result.Message);
        Assert.Equal(0x0B, msg.ConfigAck.CmdId);
    }

    [Fact]
    public void LiveMeasurement_ParsesCorrectlyWithConfig()
    {
        byte[] payload = [0x11, 0x07, 0x00, 0x01, 0xF4, 0x02, 0x00, 0x00, 0x03, 0xE8];

        var result = PayloadDecoder.Decode(payload, config: PulseConfigL1());

        Assert.True(result.IsSuccess);
        var msg = Assert.IsType<DecodedMessage.MeasurementMessage>(result.Message);
        Assert.False(msg.IsBacklog);
        Assert.Single(msg.Samples);
        Assert.Equal(500, msg.Samples[0].SeqNumber);
        Assert.Equal(1000UL, msg.Samples[0].Data.Readings[0].Value.AsUnsigned);
    }

    [Fact]
    public void BacklogMeasurement_ParsesCorrectlyWithConfig()
    {
        byte[] payload =
        [
            0x12, 0x07, 0x01, 0x00, 0x64,
            0x02,
            0x02, 0x00, 0x00, 0x03, 0xE8,
            0x02, 0x00, 0x00, 0x07, 0xD0
        ];

        var result = PayloadDecoder.Decode(payload, config: PulseConfigL1());

        Assert.True(result.IsSuccess);
        var msg = Assert.IsType<DecodedMessage.MeasurementMessage>(result.Message);
        Assert.True(msg.IsBacklog);
        Assert.Equal(2, msg.Samples.Count);
        Assert.Equal(100, msg.Samples[0].SeqNumber);
        Assert.Equal(101, msg.Samples[1].SeqNumber);
    }

    [Fact]
    public void Measurement_NoConfig_ReturnsNoConfig()
    {
        byte[] payload = [0x11, 0x07, 0x00, 0x01, 0xF4, 0x02, 0x00, 0x00, 0x03, 0xE8];

        var result = PayloadDecoder.Decode(payload, config: null);

        Assert.False(result.IsSuccess);
        Assert.Equal(DecodeError.MissingConfig, result.Error);
    }

    [Fact]
    public void EmptyPayload_ReturnsEmpty()
    {
        var result = PayloadDecoder.Decode(ReadOnlySpan<byte>.Empty, config: null);

        Assert.False(result.IsSuccess);
        Assert.Equal(DecodeError.Empty, result.Error);
    }

    [Fact]
    public void Downlink_ReturnsUnsupportedMessageType()
    {
        byte[] payload = [0x18, 0x01, 0x05, 0x02];

        var result = PayloadDecoder.Decode(payload, config: null);

        Assert.False(result.IsSuccess);
        Assert.Equal(DecodeError.UnsupportedMessageType, result.Error);
    }

    [Fact]
    public void UnasignedType_ReturnsMalformed()
    {
        byte[] payload = [0x16, 0x00];

        var result = PayloadDecoder.Decode(payload, config: null);

        Assert.False(result.IsSuccess);
        Assert.Equal(DecodeError.Malformed, result.Error);
    }

    [Fact]
    public void TruncatedPayload_ReturnsMalformed()
    {
        byte[] payload = [0x11, 0x07, 0x00, 0x01, 0xF4, 0x02, 0x00, 0x00];

        var result = PayloadDecoder.Decode(payload, config: PulseConfigL1());

        Assert.False(result.IsSuccess);
        Assert.Equal(DecodeError.Malformed, result.Error);
    }
}