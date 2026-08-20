using System.Text.Json.Serialization;

namespace Ingest.Core.Config;

public sealed class NodeConfig
{
    [JsonPropertyName("schema")]
    public int Schema { get; init; }

    [JsonPropertyName("mode")]
    public AcquisitionMode Mode { get; init; }

    [JsonPropertyName("config_version")]
    public byte ConfigVersion { get; init; }

    [JsonPropertyName("config_hash")]
    public ushort ConfigHash { get; init; }

    [JsonPropertyName("phase_config")]
    public PhaseConfig PhaseConfig { get; init; } = new();

    [JsonPropertyName("summary_mode")]
    public bool SummaryMode { get; init; }

    [JsonPropertyName("atm_channels")]
    public IReadOnlyList<AtmPhaseChannels> AtmChannels { get; init; } = [];

    [JsonPropertyName("pulse_counters")]
    public IReadOnlyList<PulseCounter> PulseCounters { get; init; } = [];

    [JsonPropertyName("modbus_registers")]
    public IReadOnlyList<ModbusRegister> ModbusRegisters { get; init; } = [];

    [JsonPropertyName("modbus")]
    public ModbusBusParams? Modbus { get; init; }
}