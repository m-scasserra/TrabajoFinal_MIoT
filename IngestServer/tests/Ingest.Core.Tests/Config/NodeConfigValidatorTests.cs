using Ingest.Core.Config;
using Ingest.Core.Protocol;

namespace Ingest.Core.Tests.Config;

public class NodeConfigValidatorTests
{
    private static NodeConfig ValidModbus() => new()
    {
        Schema = 1,
        Mode = AcquisitionMode.Modbus,
        ConfigVersion = 4,
        ConfigHash = 40762,
        PhaseConfig = new PhaseConfig
        {
            RawByte = 0x0F,
            ThreePhase = true,
            PhaseBits = 7,
            ActivePhases = [PhaseTag.L1, PhaseTag.L2, PhaseTag.L3]
        },
        Modbus = new ModbusBusParams
        {
            SlaveId = 1,
            Baudrate = 9600,
            Parity = ModbusParity.None,
        },
        ModbusRegisters =
        [
            new ModbusRegister
            {
                Index = 0,
                RegAddr = 0,
                FnCode = 3,
                PhaseTag = PhaseTag.L1,
                Value = new RegisterType
                {
                    RegCount = 1,
                    ByteWidth = 2,
                    Type = "int",
                },
                Scale = 0.1,
            },
            new ModbusRegister
            {
                Index = 1,
                RegAddr = 2,
                FnCode = 3,
                PhaseTag = PhaseTag.L2,
                Value = new RegisterType
                {
                    RegCount = 1,
                    ByteWidth = 2,
                    Type = "int",
                },
                Scale = 0.1,
            },
            new ModbusRegister
            {
                Index = 2,
                RegAddr = 4,
                FnCode = 3,
                PhaseTag = PhaseTag.L3,
                Value = new RegisterType
                {
                    RegCount = 1,
                    ByteWidth = 2,
                    Type = "int",
                },
                Scale = 0.1,
            },
        ],
    };

    private static NodeConfig ValidAtmMono() => new()
    {
        Schema = 1,
        Mode = AcquisitionMode.ATM90E32AS,
        ConfigVersion = 3,
        PhaseConfig = new PhaseConfig
        {
            RawByte = 0x02,
            ThreePhase = false,
            PhaseBits = 1,
            ActivePhases = [PhaseTag.L1]
        },
        AtmChannels = [new AtmPhaseChannels { Phase = PhaseTag.L1, Channels = ["voltage_rms"] }],
    };

    [Fact]
    public void ThreePhaseModbus_Valid_NoErrors()
    {
        var result = NodeConfigValidator.Validate(ValidModbus());
        Assert.True(result.IsValid);
    }

    [Fact]
    public void AtmOnePhase_Valid_NoErrors()
    {
        var result = NodeConfigValidator.Validate(ValidAtmMono());
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Modbus_NoRegisters_Invalid()
    {
        var config = ValidModbus();
        var broken = new NodeConfig
        {
            Schema = config.Schema,
            Mode = config.Mode,
            ConfigVersion = config.ConfigVersion,
            ConfigHash = config.ConfigHash,
            PhaseConfig = config.PhaseConfig,
            Modbus = config.Modbus,
        };

        var result = NodeConfigValidator.Validate(broken);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("Mode is Modbus but modbus_registers is empty."));
    }

    [Fact]
    public void ModeModbus_WithAtmTablePresent_Invalid()
    {
        var config = ValidModbus();
        var broken = new NodeConfig
        {
            Schema = 1,
            Mode = AcquisitionMode.Modbus,
            ConfigVersion = 4,
            PhaseConfig = config.PhaseConfig,
            Modbus = config.Modbus,
            ModbusRegisters = config.ModbusRegisters,
            AtmChannels = [new AtmPhaseChannels { Phase = PhaseTag.L1 }],
        };

        var result = NodeConfigValidator.Validate(broken);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("Mode is Modbus but atm_channels is not empty."));
    }

    [Fact]
    public void PhaseConfig_ThreePhaseWithoutPhases_Invalid()
    {
        var broken = new NodeConfig
        {
            Schema = 1,
            Mode = AcquisitionMode.Modbus,
            ConfigVersion = 4,
            PhaseConfig = new PhaseConfig
            {
                RawByte = 0x01,
                ThreePhase = true,
                PhaseBits = 0,
                ActivePhases = [],
            },
            Modbus = new ModbusBusParams
            {
                SlaveId = 1,
                Baudrate = 9600,
                Parity = ModbusParity.None,
            },
            ModbusRegisters = [ new ModbusRegister
            {
                Index= 0,
                FnCode= 3,
                PhaseTag= PhaseTag.General,
                Value = new RegisterType
                {
                    RegCount = 1,
                    ByteWidth = 2,
                },
            }],
        };

        var result = NodeConfigValidator.Validate(broken);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("phase_config not consistent: three_phase is true but phase_bits=0."));
    }

    [Fact]
    public void PhaseConfig_PhaseBitsMismatchRawByte_Invalid()
    {
        var config = ValidAtmMono();
        var broken = new NodeConfig
        {
            Schema = 1,
            Mode = AcquisitionMode.ATM90E32AS,
            ConfigVersion = 3,
            PhaseConfig = new PhaseConfig
            {
                RawByte = 0x02,
                ThreePhase = false,
                PhaseBits = 3,
                ActivePhases = [PhaseTag.L1],
            },
            AtmChannels = config.AtmChannels,
        };

        var result = NodeConfigValidator.Validate(broken);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("phase_config not consistent: phase_bits"));
    }

    [Fact]
    public void Atm_TablePhasesMismatchActivePhases_Invalid()
    {
        var broken = new NodeConfig
        {
            Schema = 1,
            Mode = AcquisitionMode.ATM90E32AS,
            ConfigVersion = 3,
            PhaseConfig = new PhaseConfig
            {
                RawByte = 0x0F,
                ThreePhase = true,
                PhaseBits = 7,
                ActivePhases = [PhaseTag.L1, PhaseTag.L2, PhaseTag.L3],
            },
            AtmChannels =
            [
                new AtmPhaseChannels {Phase = PhaseTag.L1},
                new AtmPhaseChannels {Phase = PhaseTag.L2},
            ],
        };

        var result = NodeConfigValidator.Validate(broken);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("do not match active_phase"));
    }

    [Fact]
    public void Modbus_IndexSkipping_Invalid()
    {
        var broken = new NodeConfig
        {
            Schema = 1,
            Mode = AcquisitionMode.Modbus,
            ConfigVersion = 4,
            PhaseConfig = new PhaseConfig
            {
                RawByte = 0x0F,
                ThreePhase = true,
                PhaseBits = 7,
                ActivePhases = [PhaseTag.L1, PhaseTag.L2, PhaseTag.L3],
            },
            Modbus = new ModbusBusParams { SlaveId = 1, Baudrate = 9600, Parity = ModbusParity.None, },
            ModbusRegisters =
            [
                new ModbusRegister { Index = 0, FnCode = 3, PhaseTag = PhaseTag.L1,
                    Value = new RegisterType { RegCount = 1, ByteWidth = 2, },
                },
                new ModbusRegister { Index = 2, FnCode = 3,  PhaseTag = PhaseTag.L3,
                    Value = new RegisterType { RegCount = 1, ByteWidth = 2, },
                },
            ],
        };

        var result = NodeConfigValidator.Validate(broken);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("Missing modbus_register index"));
    }

    [Fact]
    public void Modbus_ByteWidthInconsistent_Invalid()
    {
        var broken = new NodeConfig
        {
            Schema = 1,
            Mode = AcquisitionMode.Modbus,
            ConfigVersion = 4,
            PhaseConfig = new PhaseConfig
            {
                RawByte = 0x02,
                ThreePhase = false,
                PhaseBits = 1,
                ActivePhases = [PhaseTag.L1],
            },
            Modbus = new ModbusBusParams { SlaveId = 1, Baudrate = 9600, Parity = ModbusParity.None, },
            ModbusRegisters =
            [
                new ModbusRegister { Index = 0, FnCode = 3, PhaseTag = PhaseTag.L1,
                    Value = new RegisterType { RegCount = 2, ByteWidth = 2, },
                },
            ],
        };

        var result = NodeConfigValidator.Validate(broken);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("Expected byte_width="));
    }
}