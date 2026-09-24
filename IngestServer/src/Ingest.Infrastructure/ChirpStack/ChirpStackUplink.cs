using System.Text.Json.Serialization;

namespace Ingest.Infrastructure.ChirpStack;

public sealed class ChirpStackUplink
{
    [JsonPropertyName("deviceInfo")]
    public ChirpStackDeviceInfo? DeviceInfo { get; init; }

    [JsonPropertyName("time")]
    public DateTimeOffset? Time { get; init; }

    [JsonPropertyName("fCnt")]
    public uint FCnt { get; init; }

    [JsonPropertyName("fPort")]
    public int FPort { get; init; }

    [JsonPropertyName("data")]
    public string? Data { get; init; }

    [JsonPropertyName("rxInfo")]
    public IReadOnlyList<ChirpStackRxInfo>? RxInfo { get; init; }
}

public sealed class ChirpStackDeviceInfo
{
    [JsonPropertyName("devEui")]
    public string? DevEui { get; init; }

    [JsonPropertyName("deviceName")]
    public string? DeviceName { get; init; }
}

public sealed class ChirpStackRxInfo
{
    [JsonPropertyName("gatewayId")]
    public string? GatewayId { get; init; }

    [JsonPropertyName("rssi")]
    public double? Rssi { get; init; }

    [JsonPropertyName("snr")]
    public double? Snr { get; init; }
}