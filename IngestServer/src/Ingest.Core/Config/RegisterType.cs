using System.Text.Json.Serialization;

namespace Ingest.Core.Config;

public sealed class RegisterType
{
    [JsonPropertyName("reg_count")]
    public int RegCount { get; init; }

    [JsonPropertyName("signed")]
    public bool Signed { get; init; }

    [JsonPropertyName("word_swap")]
    public bool WordSwap { get; init; }

    [JsonPropertyName("type")]
    public string Type { get; init; } = "int";

    [JsonPropertyName("byte_width")]
    public int ByteWidth { get; init; }

    [JsonIgnore]
    public bool IsFloat => string.Equals(Type, "float", StringComparison.OrdinalIgnoreCase);
}