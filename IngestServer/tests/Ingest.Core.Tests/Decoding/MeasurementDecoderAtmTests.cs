using Ingest.Core.Config;
using Ingest.Core.Decoding;
using Ingest.Core.Protocol;
using Ingest.Core.Protocol.Messages;

namespace Ingest.Core.Tests.Decoding;

public class MeasurementDecoderAtmTests
{
    private static NodeConfig AtmConfig(byte rawByte, byte phaseBits, bool threePhase,
        params PhaseTag[] phases)
    {
        var channels = phases
            .Select(p => new AtmPhaseChannels { Phase = p, Channels = ["voltage_rms"] })
            .ToArray();

        return new NodeConfig
        {
            Schema = 1,
            Mode = AcquisitionMode.ATM90E32AS,
            ConfigVersion = 3,
            PhaseConfig = new PhaseConfig
            {
                RawByte = rawByte,
                ThreePhase = threePhase,
                PhaseBits = phaseBits,
                ActivePhases = phases,
            },
            AtmChannels = channels,
        };
    }

    private static MeasurementHeader Header(ushort seq, bool backlog) => new(
        ConfigVersion: 7,
        Flags: MeasurementFlags.FromByte((byte)(backlog ? 0x01 : 0x00)),
        SeqNumber: seq);

    [Fact]
    public void Atm_MonoPhase_L1_DecodeGroup()
    {
        var config = AtmConfig(0x02, 0x01, false, PhaseTag.L1);

        byte[] block =
        [
            0x02,                   // phase_maks = L1
            0x08, 0xFC,             // V = 2300
            0x02, 0x26,             // I = 550
            0x00, 0x01, 0xEE, 0x24, // P = 126500
            0x03, 0xD4,             // PF = 980
        ];

        var reader = new PayloadReader(block);
        var samples = MeasurementDecoder.Decode(
            ref reader, Header(500, backlog: false), MessageType.Measurement, config);

        Assert.Equal(0x02, samples[0].Data.PhaseMask.Raw);
        Assert.Equal(4, samples[0].Data.Readings.Count);

        var v = samples[0].Data.Readings[0];
        Assert.Equal("voltage_rms", v.Measurand);
        Assert.Equal(PhaseTag.L1, v.Phase);
        Assert.Equal(2300UL, v.Value.AsUnsigned);
        Assert.Equal(230.0, v.PhysicalValue, precision: 6);
        Assert.Equal("V", v.Unit);

        Assert.Equal("current_rms", samples[0].Data.Readings[1].Measurand);
        Assert.Equal(550UL, samples[0].Data.Readings[1].Value.AsUnsigned);
        Assert.Equal(5.50, samples[0].Data.Readings[1].PhysicalValue, precision: 6);

        Assert.Equal("active_power", samples[0].Data.Readings[2].Measurand);
        Assert.Equal(126500, samples[0].Data.Readings[2].Value.AsSigned);
        Assert.Equal(126.5, samples[0].Data.Readings[2].PhysicalValue, precision: 6);

        Assert.Equal("power_factor", samples[0].Data.Readings[3].Measurand);
        Assert.Equal(980, samples[0].Data.Readings[3].Value.AsSigned);
        Assert.Equal(0.980, samples[0].Data.Readings[3].PhysicalValue, precision: 6);

        Assert.Equal(0, reader.Remaining);
    }

    [Fact]
    public void Atm_SignedValues_L1_CorrectInterpretation()
    {
        var config = AtmConfig(0x02, 0x01, false, PhaseTag.L1);

        byte[] block =
        [
            0x02,                   // phase_maks = L1
            0x08, 0xFC,             // V = 2300
            0x02, 0x26,             // I = 550
            0xFF, 0xFE, 0x11, 0xDC, // P = -126500
            0xFC, 0x2C,             // PF = -980
        ];

        var reader = new PayloadReader(block);
        var samples = MeasurementDecoder.Decode(
            ref reader, Header(500, backlog: false), MessageType.Measurement, config);

        Assert.Equal("active_power", samples[0].Data.Readings[2].Measurand);
        Assert.Equal(-126500, samples[0].Data.Readings[2].Value.AsSigned);
        Assert.Equal(-126.5, samples[0].Data.Readings[2].PhysicalValue, precision: 6);

        Assert.Equal("power_factor", samples[0].Data.Readings[3].Measurand);
        Assert.Equal(-980, samples[0].Data.Readings[3].Value.AsSigned);
        Assert.Equal(-0.980, samples[0].Data.Readings[3].PhysicalValue, precision: 6);
    }

    [Fact]
    public void Atm_ThreePhase_DecodeGroupsInOrder()
    {
        var config = AtmConfig(0x0F, 0x07, true, PhaseTag.L1, PhaseTag.L2, PhaseTag.L3);

        byte[] block =
        [
            0x0F,                   // phase_maks = L1+L2+L3
            0x08, 0xFC, 0x02, 0x26, 0x00, 0x01, 0xEE, 0x44, 0x03, 0xD4, // L1
            0x08, 0xE6, 0x02, 0x30, 0x00, 0x01, 0xEF, 0x00, 0x03, 0xD0, // L2
            0x09, 0x10, 0x02, 0x1C, 0x00, 0x01, 0xEE, 0x00, 0x03, 0xD8, // L3
        ];

        var reader = new PayloadReader(block);
        var samples = MeasurementDecoder.Decode(
            ref reader, Header(500, backlog: false), MessageType.Measurement, config);

        Assert.Equal(12, samples[0].Data.Readings.Count);

        Assert.Equal(PhaseTag.L1, samples[0].Data.Readings[0].Phase);
        Assert.Equal(PhaseTag.L2, samples[0].Data.Readings[4].Phase);
        Assert.Equal(PhaseTag.L3, samples[0].Data.Readings[8].Phase);

        Assert.Equal(2278UL, samples[0].Data.Readings[4].Value.AsUnsigned);
        Assert.Equal("voltage_rms", samples[0].Data.Readings[4].Measurand);

        Assert.Equal(0, reader.Remaining);
    }

    [Fact]
    public void Atm_BadPayload_ThrowsPayloadFormatException()
    {
        var config = AtmConfig(0x02, 0x01, false, PhaseTag.L1);

        byte[] block = [0x02, 0x08, 0xFC, 0x02, 0x26, 0x00, 0x01, 0xEE, 0x44];

        static void Act(byte[] b, NodeConfig c)
        {
            var reader = new PayloadReader(b);
            MeasurementDecoder.Decode(ref reader, Header(500, backlog: false), MessageType.Measurement, c);
        }

        Assert.Throws<PayloadFormatException>(() => Act(block, config));
    }
}