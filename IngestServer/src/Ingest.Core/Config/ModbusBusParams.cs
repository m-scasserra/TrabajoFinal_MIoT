using System.Text.Json.Serialization;

namespace Ingest.Core.Config;

public enum ModbusParity : byte
{
    None,
    Odd,
    Even
}

public sealed class ModbusBusParams
{
    [JsonPropertyName("slave_id")]
    public byte SlaveId { get; init; }

    [JsonPropertyName("baudrate")]
    public int Baudrate { get; init; }

    [JsonPropertyName("parity")]
    public ModbusParity Parity { get; init; }
}