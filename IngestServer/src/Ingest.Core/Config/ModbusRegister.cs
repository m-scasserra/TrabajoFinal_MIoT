using System.Text.Json.Serialization;
using Ingest.Core.Protocol;

namespace Ingest.Core.Config;

public sealed class ModbusRegister
{
    [JsonPropertyName("index")]
    public int Index { get; init; }

    [JsonPropertyName("reg_addr")]
    public ushort RegAddr { get; init; }


    [JsonPropertyName("fn_code")]
    public byte FnCode { get; init; }


    [JsonPropertyName("phase_tag")]
    public PhaseTag PhaseTag { get; init; }


    [JsonPropertyName("value")]
    public RegisterType Value { get; init; } = new();

    [JsonPropertyName("scale")]
    public double Scale { get; init; }

    [JsonPropertyName("measurand")]
    public string? Measurand { get; init; }

    [JsonPropertyName("unit")]
    public string? Unit { get; init; }
}