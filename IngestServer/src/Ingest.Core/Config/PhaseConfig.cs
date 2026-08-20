using System.Text.Json.Serialization;
using Ingest.Core.Protocol;

namespace Ingest.Core.Config;

public sealed class PhaseConfig
{
    [JsonPropertyName("raw_byte")]
    public byte RawByte { get; init; }

    [JsonPropertyName("three_phase")]
    public bool ThreePhase { get; init; }

    [JsonPropertyName("phase_bits")]
    public byte PhaseBits { get; init; }

    [JsonPropertyName("active_phases")]
    public IReadOnlyList<PhaseTag> ActivePhases { get; init; } = [];
}