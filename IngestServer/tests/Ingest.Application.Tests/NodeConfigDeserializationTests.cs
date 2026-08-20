using System.Text.Json;
using System.Text.Json.Serialization;
using Ingest.Core.Config;
using Ingest.Core.Protocol;

namespace Ingest.Core.Tests.Config;

public class NodeConfigDeserializationTests
{
    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters =
        {
            new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: true)
        }
    };

    [Fact]
    public void Deserialization_ConfigModbusThreePhase()
    {
        const string json =
        """
        {
          "schema": 1,
          "mode": "Modbus",
          "config_version": 4,
          "config_hash": 40762,
          "phase_config": {
            "raw_byte": 15,
            "three_phase": true,
            "phase_bits": 7,
            "active_phases": ["L1", "L2", "L3"]
          },
          "summary_mode": false,
          "modbus": { "slave_id": 1, "baudrate": 9600, "parity": "None" },
          "modbus_registers": [
            {
              "index": 0,"reg_addr": 0,"fn_code": 3,"phase_tag": "L1",
              "value": { "reg_count": 1, "signed": false, "word_swap": false, "type": "int", "byte_width": 2 },
              "scale": 0.1, "measurand": "voltage_rms", "unit": "V"
            },
            {
              "index": 1,"reg_addr": 2,"fn_code": 3,"phase_tag": "L2",
              "value": { "reg_count": 1, "signed": false, "word_swap": false, "type": "int", "byte_width": 2 },
              "scale": 0.1, "measurand": "voltage_rms", "unit": "V"
            },
            {
              "index": 2,"reg_addr": 4,"fn_code": 3,"phase_tag": "L3",
              "value": { "reg_count": 1, "signed": false, "word_swap": false, "type": "int", "byte_width": 2 },
              "scale": 0.1, "measurand": "voltage_rms", "unit": "V"
            }
          ]
        }
        """;

        var config = JsonSerializer.Deserialize<NodeConfig>(json, Options);

        Assert.NotNull(config);
        Assert.Equal(1, config!.Schema);
        Assert.Equal(AcquisitionMode.Modbus, config.Mode);
        Assert.Equal(4, config.ConfigVersion);
        Assert.Equal(40762, config.ConfigHash);
        Assert.False(config.SummaryMode);

        Assert.Equal(15, config.PhaseConfig.RawByte);
        Assert.True(config.PhaseConfig.ThreePhase);
        Assert.Equal(7, config.PhaseConfig.PhaseBits);
        Assert.Equal(
            new[] { PhaseTag.L1, PhaseTag.L2, PhaseTag.L3 },
            config.PhaseConfig.ActivePhases);

        Assert.NotNull(config.Modbus);
        Assert.Equal(1, config.Modbus.SlaveId);
        Assert.Equal(9600, config.Modbus.Baudrate);
        Assert.Equal(ModbusParity.None, config.Modbus.Parity);

        Assert.Equal(3, config.ModbusRegisters.Count);
        Assert.Empty(config.AtmChannels);
        Assert.Empty(config.PulseCounters);

        var r0 = config.ModbusRegisters[0];
        Assert.Equal(0, r0.Index);
        Assert.Equal(0, r0.RegAddr);
        Assert.Equal(3, r0.FnCode);
        Assert.Equal(PhaseTag.L1, r0.PhaseTag);
        Assert.Equal(0.1, r0.Scale, precision: 10);
        Assert.Equal("voltage_rms", r0.Measurand);
        Assert.Equal(1, r0.Value.RegCount);
        Assert.False(r0.Value.Signed);
        Assert.False(r0.Value.IsFloat);
        Assert.Equal(2, r0.Value.ByteWidth);

        Assert.Equal(PhaseTag.L2, config.ModbusRegisters[1].PhaseTag);
        Assert.Equal(PhaseTag.L3, config.ModbusRegisters[2].PhaseTag);
    }

    [Fact]
    public void Deserialization_ConfigAtmMonoPhase()
    {
        const string json =
        """
        {
          "schema": 1,
          "mode": "Atm90E32As",
          "config_version": 3,
          "config_hash": 6699,
          "phase_config": {
            "raw_byte": 2,
            "three_phase": false,
            "phase_bits": 1,
            "active_phases": ["L1"]
          },
          "summary_mode": false,
          "atm_channels": [
            { "phase": "L1", "channels": ["voltage_rms", "current_rms", "active_power", "power_factor"] }
          ]
        }
        """;

        var config = JsonSerializer.Deserialize<NodeConfig>(json, Options);

        Assert.NotNull(config);
        Assert.Equal(1, config!.Schema);
        Assert.Equal(AcquisitionMode.ATM90E32AS, config!.Mode);
        Assert.Single(config.AtmChannels);
        Assert.Equal(PhaseTag.L1, config.AtmChannels[0].Phase);
        Assert.Equal(4, config.AtmChannels[0].Channels.Count);
        Assert.Empty(config.ModbusRegisters);
        Assert.Empty(config.PulseCounters);
        Assert.Null(config.Modbus);
    }

    [Fact]
    public void Deserialization_ConfigPulseThreePhase()
    {
        const string json =
        """
        {
          "schema": 1,
          "mode": "Pulse",
          "config_version": 7,
          "config_hash": 50393,
          "phase_config": {
            "raw_byte": 15,
            "three_phase": true,
            "phase_bits": 7,
            "active_phases": ["L1", "L2", "L3"]
          },
          "summary_mode": false,
          "pulse_counters": [
            { "phase": "L1", "pulses_per_kwh": 1000 },
            { "phase": "L2", "pulses_per_kwh": 1000 },
            { "phase": "L3", "pulses_per_kwh": 1000 }
          ]
        }
        """;

        var config = JsonSerializer.Deserialize<NodeConfig>(json, Options);

        Assert.NotNull(config);
        Assert.Equal(AcquisitionMode.Pulse, config!.Mode);
        Assert.Equal(3, config.PulseCounters.Count);
        Assert.Equal(1000, config.PulseCounters[0].PulsesPerKwh);
        Assert.Empty(config.AtmChannels);
        Assert.Empty(config.ModbusRegisters);
    }
}