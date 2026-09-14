using Ingest.Core.Config;
using Ingest.Core.Protocol;
using Ingest.Core.Protocol.Messages;

namespace Ingest.Core.Decoding;

public static class MeasurementDecoder
{
    public static IReadOnlyList<MeasurementSample> Decode(
        ref PayloadReader reader,
        MeasurementHeader header,
        MessageType messageType,
        NodeConfig config)

    {
        return messageType switch
        {
            MessageType.Measurement => DecodeLive(ref reader, header, config),
            MessageType.MeasurementBacklog => DecodeBacklog(ref reader, header, config),
            _ => throw new PayloadFormatException(
                $"Unsupported measurement message type: {(int)messageType}."),
        };
    }

    private static IReadOnlyList<MeasurementSample> DecodeLive(
        ref PayloadReader reader, MeasurementHeader header, NodeConfig config)
    {
        var block = DecodeBlock(ref reader, config);
        return [new MeasurementSample { SeqNumber = header.SeqNumber, Data = block }];
    }

    private static IReadOnlyList<MeasurementSample> DecodeBacklog(
        ref PayloadReader reader, MeasurementHeader header, NodeConfig config)
    {
        byte sampleCount = reader.ReadU8();
        if (sampleCount == 0)
        {
            throw new PayloadFormatException("Backlog sample count cannot be zero.");
        }

        var samples = new List<MeasurementSample>(sampleCount);
        for (int i = 0; i < sampleCount; i++)
        {
            var block = DecodeBlock(ref reader, config);
            ushort seq = (ushort)(header.SeqNumber + i);
            samples.Add(new MeasurementSample { SeqNumber = seq, Data = block });
        }

        return samples;
    }

    private static MeasurementData DecodeBlock(ref PayloadReader reader, NodeConfig config)
    {
        return config.Mode switch
        {
            AcquisitionMode.ATM90E32AS => DecodeAtm(ref reader, config),
            AcquisitionMode.Pulse => DecodePulse(ref reader, config),
            AcquisitionMode.Modbus => DecodeModbus(ref reader, config),
            _ => throw new PayloadFormatException($"Unsupported acquisition mode: {(int)config.Mode}."),
        };
    }

    private const double VoltageScale = 0.1;
    private const double CurrentScale = 0.01;
    private const double PowerScale = 0.001;
    private const double PowerFactorScale = 0.001;

    private static MeasurementData DecodeAtm(ref PayloadReader reader, NodeConfig config)
    {
        var mask = PhaseMask.FromByte(reader.ReadU8());

        var readings = new List<MeasurementReading>(mask.ActivePhaseCount * 4);

        foreach (var phase in mask.ActivePhases())
        {
            ushort voltage = reader.ReadU16();
            ushort current = reader.ReadU16();
            int power = reader.ReadI32();
            short powerFactor = reader.ReadI16();

            readings.Add(new MeasurementReading("voltage_rms", phase,
                MeasurementValue.FromUnsigned(voltage), VoltageScale, "V"));
            readings.Add(new MeasurementReading("current_rms", phase,
                MeasurementValue.FromUnsigned(current), CurrentScale, "A"));
            readings.Add(new MeasurementReading("active_power", phase,
                MeasurementValue.FromSigned(power), PowerScale, "kW"));
            readings.Add(new MeasurementReading("power_factor", phase,
                MeasurementValue.FromSigned(powerFactor), PowerFactorScale, null));
        }

        return new MeasurementData { PhaseMask = mask, Readings = readings };
    }

    private static MeasurementData DecodePulse(ref PayloadReader reader, NodeConfig config)
    {
        var mask = PhaseMask.FromByte(reader.ReadU8());

        var readings = new List<MeasurementReading>(mask.ActivePhaseCount);

        foreach (var phase in mask.ActivePhases())
        {
            uint pulseCount = reader.ReadU32();
            readings.Add(new MeasurementReading("pulse_count", phase,
                MeasurementValue.FromUnsigned(pulseCount), 1.0, "pulses"));
        }

        return new MeasurementData { PhaseMask = mask, Readings = readings };
    }

    private static MeasurementData DecodeModbus(ref PayloadReader reader, NodeConfig config)
    {
        var mask = PhaseMask.FromByte(reader.ReadU8());

        var registers = config.ModbusRegisters.OrderBy(r => r.Index);

        var readings = new List<MeasurementReading>(config.ModbusRegisters.Count);

        Span<byte> canonical = new byte[8];
        foreach (var reg in registers)
        {
            int width = reg.Value.ByteWidth;
            var slot = canonical[..width];

            RegisterBytes.ReadCanonical(ref reader, width, reg.Value.WordSwap, slot);
            MeasurementValue value = RegisterValue.Interpret(slot, reg.Value);

            readings.Add(new MeasurementReading(
                Measurand: reg.Measurand ?? $"reg_{reg.RegAddr:X4}",
                Phase: reg.PhaseTag,
                Value: value,
                Scale: reg.Scale,
                Unit: reg.Unit));
        }
        return new MeasurementData { PhaseMask = mask, Readings = readings };
    }
}