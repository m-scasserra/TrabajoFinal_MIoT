using Ingest.Core.Config;
using Ingest.Core.Decoding;
using Ingest.Core.Protocol;
using Ingest.Core.Protocol.Messages;

namespace Ingest.Core.Tests.Decoding;

public class MeasurementDecoderBacklogTests
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

    private static MeasurementHeader Header(ushort seq, bool backlog) => new(
        ConfigVersion: 7,
        Flags: MeasurementFlags.FromByte((byte)(backlog ? 0x01 : 0x00)),
        SeqNumber: seq);

    [Fact]
    public void Live_ProducesOneSample()
    {
        var config = PulseConfigL1();

        byte[] block =
        [
            0x02,                   // phase_maks = L1
            0x00, 0x00, 0x03, 0xE8, // 1000 pulses
        ];

        var reader = new PayloadReader(block);
        var samples = MeasurementDecoder.Decode(
            ref reader, Header(500, backlog: false), MessageType.Measurement, config);

        Assert.Single(samples);
        Assert.Equal(500, samples[0].SeqNumber);
        Assert.Equal(1000UL, samples[0].Data.Readings[0].Value.AsUnsigned);
        Assert.Equal(0, reader.Remaining);
    }

    [Fact]
    public void Backlog_ThreeSamples()
    {
        var config = PulseConfigL1();

        byte[] payload =
        [
            0x03,                           // sample_count = 3
            0x02, 0x00, 0x00, 0x03, 0xE8,   // L1 + 1000 pulses
            0x02, 0x00, 0x00, 0x07, 0xD0,   // L1 + 2000 pulses
            0x02, 0x00, 0x00, 0x0B, 0xB8,   // L1 + 3000 pulses
        ];

        var reader = new PayloadReader(payload);
        var samples = MeasurementDecoder.Decode(
            ref reader, Header(100, backlog: true), MessageType.MeasurementBacklog, config);

        Assert.Equal(3, samples.Count);

        Assert.Equal(100, samples[0].SeqNumber);
        Assert.Equal(101, samples[1].SeqNumber);
        Assert.Equal(102, samples[2].SeqNumber);

        Assert.Equal(1000UL, samples[0].Data.Readings[0].Value.AsUnsigned);
        Assert.Equal(2000UL, samples[1].Data.Readings[0].Value.AsUnsigned);
        Assert.Equal(3000UL, samples[2].Data.Readings[0].Value.AsUnsigned);

        Assert.Equal(0, reader.Remaining);
    }

    [Fact]
    public void Backlog_OneSampleValid()
    {
        var config = PulseConfigL1();
        byte[] payload =
        [
            0x01,                           // sample_count = 3
            0x02, 0x00, 0x00, 0x03, 0xE8,   // L1 + 1000 pulses
        ];

        var reader = new PayloadReader(payload);
        var samples = MeasurementDecoder.Decode(
            ref reader, Header(42, backlog: true), MessageType.MeasurementBacklog, config);

        Assert.Single(samples);
        Assert.Equal(42, samples[0].SeqNumber);
    }

    [Fact]
    public void Backlog_SeqNumberWrap()
    {
        var config = PulseConfigL1();
        byte[] payload =
        [
            0x03,                           // sample_count = 3
            0x02, 0x00, 0x00, 0x03, 0xE8,   // L1 + 1000 pulses
            0x02, 0x00, 0x00, 0x07, 0xD0,   // L1 + 2000 pulses
            0x02, 0x00, 0x00, 0x0B, 0xB8,   // L1 + 3000 pulses
        ];

        var reader = new PayloadReader(payload);
        var samples = MeasurementDecoder.Decode(
            ref reader, Header(65534, backlog: true), MessageType.MeasurementBacklog, config);

        Assert.Equal(65534, samples[0].SeqNumber);
        Assert.Equal(65535, samples[1].SeqNumber);
        Assert.Equal(0, samples[2].SeqNumber);
    }

    [Fact]
    public void Backlog_SampleCountThrows()
    {
        var config = PulseConfigL1();
        byte[] payload = [0x00];

        static void Act(byte[] b, NodeConfig c)
        {
            var reader = new PayloadReader(b);
            var samples = MeasurementDecoder.Decode(
                ref reader, Header(7, backlog: true), MessageType.MeasurementBacklog, c);
        }

        Assert.Throws<PayloadFormatException>(() => Act(payload, config));
    }

    [Fact]
    public void Backlog_DecodeMultipleSamples()
    {
        var config = PulseConfigL1();
        byte[] payload =
        [
            0x02,                           // sample_count = 2
            0x02, 0x00, 0x00, 0x03, 0xE8,   // mask + 1000 pulses
            0x04, 0x00, 0x00, 0x07, 0xD0,   // mask + 2000 pulses
        ];

        var reader = new PayloadReader(payload);
        var samples = MeasurementDecoder.Decode(
            ref reader, Header(0, backlog: true), MessageType.MeasurementBacklog, config);

        Assert.Equal(2, samples.Count);
        Assert.Equal(0x02, samples[0].Data.PhaseMask.Raw);
        Assert.Equal(0x04, samples[1].Data.PhaseMask.Raw);
        Assert.Equal(0, reader.Remaining);
    }
}