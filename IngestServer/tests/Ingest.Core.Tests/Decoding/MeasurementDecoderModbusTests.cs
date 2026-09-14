using Ingest.Core.Config;
using Ingest.Core.Decoding;
using Ingest.Core.Protocol;
using Ingest.Core.Protocol.Messages;

namespace Ingest.Core.Tests.Decoding;

public class MeasurementDecoderModbusTests
{
    private static ModbusRegister Reg(
        int index, ushort addr, PhaseTag phase, int regCount, bool signed,
        double scale, string measurand, string type = "int", bool wordSwap = false) => new()
        {
            Index = index,
            RegAddr = addr,
            FnCode = 3,
            PhaseTag = phase,
            Scale = scale,
            Measurand = measurand,
            Value = new RegisterType
            {
                RegCount = regCount,
                ByteWidth = regCount * 2,
                Signed = signed,
                Type = type,
                WordSwap = wordSwap,
            },
        };

    private static NodeConfig ModbusConfig(byte rawByte, byte phaseBits, bool threePhase,
        PhaseTag[] activePhases, params ModbusRegister[] regs) => new()
        {
            Schema = 1,
            Mode = AcquisitionMode.Modbus,
            ConfigVersion = 4,
            PhaseConfig = new PhaseConfig
            {
                RawByte = rawByte,
                ThreePhase = threePhase,
                PhaseBits = phaseBits,
                ActivePhases = activePhases,
            },
            Modbus = new ModbusBusParams { SlaveId = 1, Baudrate = 9600, Parity = ModbusParity.None },
            ModbusRegisters = regs,
        };

    private static MeasurementHeader Header(ushort seq, bool backlog) => new(
        ConfigVersion: 7,
        Flags: MeasurementFlags.FromByte((byte)(backlog ? 0x01 : 0x00)),
        SeqNumber: seq);

    [Fact]
    public void Vector_Section10_ThreePhasesThreeVoltages()
    {
        var config = ModbusConfig(
            0x0F, 0x07, true, [PhaseTag.L1, PhaseTag.L2, PhaseTag.L3],
            Reg(0, 0x0000, PhaseTag.L1, 1, false, 0.1, "voltage_rms"),
            Reg(1, 0x0002, PhaseTag.L2, 1, false, 0.1, "voltage_rms"),
            Reg(2, 0x0004, PhaseTag.L3, 1, false, 0.1, "voltage_rms"));

        byte[] block =
        [
            0x0F,       // phase_mask
            0x08, 0xFC, // L1 voltage
            0x08, 0xE6, // L2 voltage
            0x09, 0x10  // L3 voltage
        ];

        var reader = new PayloadReader(block);
        var samples = MeasurementDecoder.Decode(
            ref reader, Header(500, backlog: false), MessageType.Measurement, config);

        Assert.Equal(0x0F, samples[0].Data.PhaseMask.Raw);
        Assert.Equal(3, samples[0].Data.Readings.Count);

        Assert.Equal(PhaseTag.L1, samples[0].Data.Readings[0].Phase);
        Assert.Equal(2300UL, samples[0].Data.Readings[0].Value.AsUnsigned);
        Assert.Equal(230.0, samples[0].Data.Readings[0].PhysicalValue, precision: 6);
        Assert.Equal("voltage_rms", samples[0].Data.Readings[0].Measurand);

        Assert.Equal(PhaseTag.L2, samples[0].Data.Readings[1].Phase);
        Assert.Equal(227.8, samples[0].Data.Readings[1].PhysicalValue, precision: 6);

        Assert.Equal(PhaseTag.L3, samples[0].Data.Readings[2].Phase);
        Assert.Equal(232.0, samples[0].Data.Readings[2].PhysicalValue, precision: 6);

        Assert.Equal(0, reader.Remaining);
    }

    [Fact]
    public void MixedWidth_U16AndU32InTheSameTable()
    {
        var config = ModbusConfig(
            0x02, 0x01, false, [PhaseTag.L1],
            Reg(0, 0x0000, PhaseTag.L1, 1, false, 0.1, "voltage_rms"),
            Reg(1, 0x0010, PhaseTag.L1, 2, false, 1.0, "energy"));

        byte[] block =
        [
            0x02,                   // phase_mask
            0x08, 0xFC,             // L1 voltage
            0x00, 0x0F, 0x42, 0x40  // L1 energy
        ];

        var reader = new PayloadReader(block);
        var samples = MeasurementDecoder.Decode(
            ref reader, Header(500, backlog: false), MessageType.Measurement, config);

        Assert.Equal(2, samples[0].Data.Readings.Count);
        Assert.Equal(2300UL, samples[0].Data.Readings[0].Value.AsUnsigned);
        Assert.Equal(1_000_000UL, samples[0].Data.Readings[1].Value.AsUnsigned);
        Assert.Equal("energy", samples[0].Data.Readings[1].Measurand);
        Assert.Equal(0, reader.Remaining);
    }

    [Fact]
    public void FloatRegister_InterpretsCorrectly()
    {
        var config = ModbusConfig(
            0x02, 0x01, false, [PhaseTag.L1],
            Reg(0, 0x0000, PhaseTag.L1, 2, false, 1.0, "power", type: "float"));

        byte[] block =
        [
            0x02,                   // phase_mask
            0x43, 0x66, 0x80, 0x00  // L1 power
        ];

        var reader = new PayloadReader(block);
        var samples = MeasurementDecoder.Decode(
            ref reader, Header(500, backlog: false), MessageType.Measurement, config);

        Assert.Equal(ValueKind.Float, samples[0].Data.Readings[0].Value.Kind);
        Assert.Equal(230.5, samples[0].Data.Readings[0].Value.AsFloat, precision: 6);
    }

    [Fact]
    public void RegisterWithWordSwap_NormalizesCorrectly()
    {
        var config = ModbusConfig(
            0x02, 0x01, false, [PhaseTag.L1],
            Reg(0, 0x0000, PhaseTag.L1, 2, false, 1.0, "energy", wordSwap: true));

        byte[] block =
        [
            0x02,                   // phase_mask
            0x22, 0x33, 0x00, 0x11  // L1 energy
        ];

        var reader = new PayloadReader(block);
        var samples = MeasurementDecoder.Decode(
            ref reader, Header(500, backlog: false), MessageType.Measurement, config);

        Assert.Equal(0x00112233UL, samples[0].Data.Readings[0].Value.AsUnsigned);
        Assert.Equal(0, reader.Remaining);
    }

    [Fact]
    public void PhaseFromPhaseTag_NotFromRegisters()
    {
        var config = ModbusConfig(
            0x0F, 0x07, true, [PhaseTag.L1, PhaseTag.L2, PhaseTag.L3],
            Reg(0, 0x0000, PhaseTag.General, 1, false, 1.0, "total_power"),
            Reg(1, 0x0002, PhaseTag.L3, 1, false, 0.1, "voltage_rms"),
            Reg(2, 0x0004, PhaseTag.L1, 1, false, 0.1, "voltage_rms"));

        byte[] block =
        [
            0x0F,       // phase_mask
            0x00, 0x64, // general power
            0x09, 0x10, // L3 voltage_rms
            0x08, 0xFC  // L1 voltage_rms
        ];

        var reader = new PayloadReader(block);
        var samples = MeasurementDecoder.Decode(
            ref reader, Header(500, backlog: false), MessageType.Measurement, config);

        Assert.Equal(PhaseTag.General, samples[0].Data.Readings[0].Phase);
        Assert.Equal("total_power", samples[0].Data.Readings[0].Measurand);
        Assert.Equal(PhaseTag.L3, samples[0].Data.Readings[1].Phase);
        Assert.Equal(PhaseTag.L1, samples[0].Data.Readings[2].Phase);
    }

    [Fact]
    public void RegistersOutOfOrder_RunsByIndex()
    {
        var config = ModbusConfig(
            0x0F, 0x07, true, [PhaseTag.L1, PhaseTag.L2, PhaseTag.L3],
            Reg(2, 0x0004, PhaseTag.L3, 1, false, 0.1, "voltage_rms"),
            Reg(0, 0x0000, PhaseTag.L1, 1, false, 0.1, "voltage_rms"),
            Reg(1, 0x0002, PhaseTag.L2, 1, false, 0.1, "voltage_rms"));

        byte[] block =
        [
            0x0F,       // phase_mask
            0x08, 0xFC, // L1 voltage_rms
            0x08, 0xE6, // L2 voltage_rms
            0x09, 0x10  // L3 voltage_rms
        ];

        var reader = new PayloadReader(block);
        var samples = MeasurementDecoder.Decode(
            ref reader, Header(500, backlog: false), MessageType.Measurement, config);

        Assert.Equal(PhaseTag.L1, samples[0].Data.Readings[0].Phase);
        Assert.Equal(2300UL, samples[0].Data.Readings[0].Value.AsUnsigned);
        Assert.Equal(PhaseTag.L2, samples[0].Data.Readings[1].Phase);
        Assert.Equal(PhaseTag.L3, samples[0].Data.Readings[2].Phase);
    }

    [Fact]
    public void RegisterMissingBytes_ThrowsPayloadFormatException()
    {
        var config = ModbusConfig(
            0x02, 0x01, false, [PhaseTag.L1],
            Reg(0, 0x0000, PhaseTag.L1, 2, false, 1.0, "energy"));

        byte[] block =
        [
            0x0F,       // phase_mask
            0x08, 0xFC, // L1 energy
        ];

        static void Act(byte[] b, NodeConfig c)
        {
            var reader = new PayloadReader(b);
            var samples = MeasurementDecoder.Decode(
                ref reader, Header(500, backlog: false), MessageType.Measurement, c);
        }

        Assert.Throws<PayloadFormatException>(() => Act(block, config));
    }
}