using System.Text.Json.Serialization;
using Ingest.Core.Protocol;

namespace Ingest.Core.Config;

public sealed class PulseCounter
{
    [JsonPropertyName("phase")]
    public PhaseTag Phase { get; init; }

    [JsonPropertyName("pulses_per_kwh")]
    public int PulsesPerKwh { get; init; }
}