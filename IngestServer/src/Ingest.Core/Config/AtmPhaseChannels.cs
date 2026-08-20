using System.Text.Json.Serialization;
using Ingest.Core.Protocol;

namespace Ingest.Core.Config;

public sealed class AtmPhaseChannels
{
    [JsonPropertyName("phase")]
    public PhaseTag Phase { get; init; }

    [JsonPropertyName("channels")]
    public IReadOnlyList<string> Channels { get; init; } = [];
}