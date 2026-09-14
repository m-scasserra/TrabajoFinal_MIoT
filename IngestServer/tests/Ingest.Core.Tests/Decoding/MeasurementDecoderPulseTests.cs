using Ingest.Core.Config;
using Ingest.Core.Decoding;
using Ingest.Core.Protocol;
using Ingest.Core.Protocol.Messages;

namespace Ingest.Core.Tests.Decoding;

public class MeasurementDecoderPulseTests
{
    private static NodeConfig PulseConfig(byte rawByte, byte phaseBits, bool threePhase,
        params PhaseTag[] phases)
    {
        var counter = phases
            .Select(p => new PulseCounter { Phase = p, PulsesPerKwh = 1000 })
            .ToArray();

        return new NodeConfig
        {
            Schema = 1,
            Mode = AcquisitionMode.Pulse,
            ConfigVersion = 7,
            PhaseConfig = new PhaseConfig
            {
                RawByte = rawByte,
                ThreePhase = threePhase,
                PhaseBits = phaseBits,
                ActivePhases = phases,
            },
            PulseCounters = counter,
        };
    }

    private static MeasurementHeader Header(ushort seq, bool backlog) => new(
        ConfigVersion: 7,
        Flags: MeasurementFlags.FromByte((byte)(backlog ? 0x01 : 0x00)),
        SeqNumber: seq);

    [Fact]
    public void Pulse_MonoPhase_DecodeOneCounter()
    {
        var config = PulseConfig(0x02, 0x01, false, PhaseTag.L1);

        byte[] block =
        [
            0x02,                   // phase_maks = L1
            0x00, 0x0F, 0x42, 0x40, // pulse_count = 1_000_000
        ];

        var reader = new PayloadReader(block);
        var samples = MeasurementDecoder.Decode(
            ref reader, Header(500, backlog: false), MessageType.Measurement, config);

        Assert.Single(samples[0].Data.Readings);
        Assert.Equal("pulse_count", samples[0].Data.Readings[0].Measurand);
        Assert.Equal(PhaseTag.L1, samples[0].Data.Readings[0].Phase);
        Assert.Equal(1_000_000UL, samples[0].Data.Readings[0].Value.AsUnsigned);
        Assert.Equal(0, reader.Remaining);
    }

    [Fact]
    public void Pulse_BigCount_NoOverflow()
    {
        var config = PulseConfig(0x02, 0x01, false, PhaseTag.L1);

        byte[] block =
        [
            0x02,                   // phase_maks = L1
            0xFF, 0xFF, 0xFF, 0xFF, // pulse_count = 4_294_967_295
        ];

        var reader = new PayloadReader(block);
        var samples = MeasurementDecoder.Decode(
            ref reader, Header(500, backlog: false), MessageType.Measurement, config);

        Assert.Equal(4_294_967_295UL, samples[0].Data.Readings[0].Value.AsUnsigned);
    }

    [Fact]
    public void Pulse_ThreePhase_DecodeInOrder()
    {
        var config = PulseConfig(0x0F, 0x07, true, PhaseTag.L1, PhaseTag.L2, PhaseTag.L3);

        byte[] block =
        [
            0x0f,                   // phase_maks = L1+L2+L3
            0x00, 0x00, 0x03, 0xE8, // L1 = 1000
            0x00, 0x00, 0x07, 0xD0, // L2 = 2000
            0x00, 0x00, 0x0B, 0xB8, // L3 = 3000
        ];

        var reader = new PayloadReader(block);
        var samples = MeasurementDecoder.Decode(
            ref reader, Header(500, backlog: false), MessageType.Measurement, config);

        Assert.Equal(3, samples[0].Data.Readings.Count);
        Assert.Equal(PhaseTag.L1, samples[0].Data.Readings[0].Phase);
        Assert.Equal(1000UL, samples[0].Data.Readings[0].Value.AsUnsigned);
        Assert.Equal(PhaseTag.L2, samples[0].Data.Readings[1].Phase);
        Assert.Equal(2000UL, samples[0].Data.Readings[1].Value.AsUnsigned);
        Assert.Equal(PhaseTag.L3, samples[0].Data.Readings[2].Phase);
        Assert.Equal(3000UL, samples[0].Data.Readings[2].Value.AsUnsigned);
        Assert.Equal(0, reader.Remaining);
    }
}