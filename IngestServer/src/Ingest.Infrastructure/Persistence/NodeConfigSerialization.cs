using System.Text.Json;
using System.Text.Json.Serialization;

namespace Ingest.Infrastructure.Persistence;

public static class NodeConfigSerialization
{
    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters =
        {
            new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: true),
        },
    };
}