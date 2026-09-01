using Ingest.Core.Protocol;

namespace Ingest.Core.Config;

public sealed class NodeConfigValidator
{
    private const int SchemaSupported = 1;

    public static ValidationResult Validate(NodeConfig config)
    {
        var result = new ValidationResult();

        ValidateSchema(config, result);
        ValidatePhaseConfig(config, result);
        ValidateModeAndTables(config, result);

        return result;
    }

    private static void ValidateSchema(NodeConfig config, ValidationResult result)
    {
        if (config.Schema != SchemaSupported)
        {
            result.Add($"Unsupported schema version: {config.Schema}. Supported version is {SchemaSupported}.");
        }
    }

    private static void ValidatePhaseConfig(NodeConfig config, ValidationResult result)
    {
        var pc = config.PhaseConfig;

        byte bitsFromRaw = (byte)((pc.RawByte >> 1) & 0b0000_0111);
        if (pc.PhaseBits != bitsFromRaw)
        {
            result.Add(
                $"phase_config not consistent: phase_bits {pc.PhaseBits} does not match " +
                $"bits 3-1 of raw_byte=0x{pc.RawByte:X2} (={bitsFromRaw}).");
        }

        bool bit0 = (pc.RawByte & 0b0000_0001) != 0;
        if (bit0 != pc.ThreePhase)
        {
            result.Add(
                $"phase_config not consistent: three_phase={pc.ThreePhase} does not match " +
                $"bit 0 of raw_byte=0x{pc.RawByte:X2}.");
        }

        if (pc.ThreePhase && pc.PhaseBits == 0)
        {
            result.Add("phase_config not consistent: three_phase is true but phase_bits=0.");
        }

        var expected = PhasesFromBits(pc.PhaseBits);
        var actual = pc.ActivePhases.ToHashSet();
        if (!expected.SetEquals(actual))
        {
            result.Add(
                $"active_phases {Fmt(pc.ActivePhases)} does not match phase_bits={pc.PhaseBits} " +
                $"(expected {Fmt(expected)}).");
        }
    }

    private static void ValidateModeAndTables(NodeConfig config, ValidationResult result)
    {
        bool hasAtm = config.AtmChannels.Count > 0;
        bool hasPulse = config.PulseCounters.Count > 0;
        bool hasModbus = config.ModbusRegisters.Count > 0;

        switch (config.Mode)
        {
            case AcquisitionMode.ATM90E32AS:
                if (!hasAtm) result.Add("Mode is ATM90E32AS but atm_channels is empty.");
                if (hasPulse) result.Add("Mode is ATM90E32AS but pulse_counters is not empty.");
                if (hasModbus) result.Add("Mode is ATM90E32AS but modbus_registers is not empty.");
                ValidatePhasesMatchTable(
                    config, config.AtmChannels.Select(c => c.Phase), result);
                break;

            case AcquisitionMode.Pulse:
                if (!hasPulse) result.Add("Mode is Pulse but pulse_counters is empty.");
                if (hasAtm) result.Add("Mode is Pulse but atm_channels is not empty.");
                if (hasModbus) result.Add("Mode is Pulse but modbus_registers is not empty.");
                ValidatePhasesMatchTable(
                    config, config.PulseCounters.Select(c => c.Phase), result);
                break;

            case AcquisitionMode.Modbus:
                if (!hasModbus) result.Add("Mode is Modbus but modbus_registers is empty.");
                if (hasAtm) result.Add("Mode is Modbus but atm_channels is not empty.");
                if (hasPulse) result.Add("Mode is Modbus but pulse_counters is not empty.");
                if (config.Modbus is null)
                    result.Add("Mode is Modbus but bus config is null.");
                ValidateModbusRegisters(config, result);
                break;

            default:
                result.Add($"Unknown acquisition mode: {config.Mode}");
                break;
        }
    }

    private static void ValidatePhasesMatchTable(
        NodeConfig config, IEnumerable<PhaseTag> tablePhases, ValidationResult result)
    {
        var phases = tablePhases.ToList();
        var active = config.PhaseConfig.ActivePhases.ToHashSet();

        var set = new HashSet<PhaseTag>();
        foreach (var p in phases)
        {
            if (!set.Add(p))
                result.Add($"Duplicate phase {p} in table.");
        }

        if (!set.SetEquals(active))
        {
            result.Add(
                $"Phases in table {Fmt(set)} do not match active_phases {Fmt(active)}.");
        }
    }

    private static void ValidateModbusRegisters(NodeConfig config, ValidationResult result)
    {
        var regs = config.ModbusRegisters;

        var seen = new HashSet<int>();
        foreach (var r in regs)
        {
            if (!seen.Add(r.Index))
                result.Add($"Duplicate modbus_register index {r.Index}.");
        }
        for (int i = 0; i < regs.Count; i++)
        {
            if (!seen.Contains(i))
                result.Add($"Missing modbus_register index {i}. Must be from 0 to {regs.Count - 1}.");
        }

        foreach (var r in regs)
        {
            if (r.Value.RegCount is not (1 or 2 or 4))
                result.Add(
                    $"modbus_register index {r.Index} has invalid value.reg_count={r.Value.RegCount}. " +
                    "Must be 1, 2, or 4.");

            int expectedWidth = r.Value.RegCount * 2;
            if (r.Value.ByteWidth != expectedWidth)
                result.Add(
                    $"byte_width ({r.Value.ByteWidth}) does not match reg_count ({r.Value.RegCount}) " +
                    $"for modbus_register index {r.Index}. Expected byte_width={expectedWidth}.");

            if (r.FnCode is not (0x03 or 0x04))
                result.Add(
                    $"modbus_register index {r.Index} has invalid fn_code=0x{r.FnCode:X2}. " +
                    "Must be 0x03 (holding) or 0x04 (input).");

            if (r.PhaseTag == PhaseTag.Unknown)
                result.Add($"modbus_register index {r.Index} has invalid phase_tag.");
        }
    }
    private static HashSet<PhaseTag> PhasesFromBits(byte phaseBits)
    {
        var set = new HashSet<PhaseTag>();
        if ((phaseBits & 0b001) != 0) set.Add(PhaseTag.L1);
        if ((phaseBits & 0b010) != 0) set.Add(PhaseTag.L2);
        if ((phaseBits & 0b100) != 0) set.Add(PhaseTag.L3);
        return set;
    }

    private static string Fmt(IEnumerable<PhaseTag> phases) =>
        "[" + string.Join(", ", phases.OrderBy(p => p)) + "]";
}