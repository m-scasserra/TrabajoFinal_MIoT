using Ingest.Application.Pipeline;

namespace Ingest.Infrastructure.ChirpStack;

public enum UplinkParseStatus
{
    Ok,
    MissingDevEui,
    MissingData,
    InvalidBase64,
}

public readonly record struct UplinkParseResult(
    UplinkParseStatus Status, UplinkContext? Context, string? Error)
{
    public static UplinkParseResult Ok(UplinkContext ctx) => new(UplinkParseStatus.Ok, ctx, null);
    public static UplinkParseResult Fail(UplinkParseStatus status, string error) =>
        new(status, null, error);
}

public static class UplinkContextFactory
{
    public static UplinkParseResult Create(ChirpStackUplink uplink)
    {
        string? devEui = uplink.DeviceInfo?.DevEui;
        if (string.IsNullOrEmpty(devEui))
        {
            return UplinkParseResult.Fail(UplinkParseStatus.MissingDevEui, "Device EUI is missing.");
        }

        if (string.IsNullOrEmpty(uplink.Data))
        {
            return UplinkParseResult.Fail(UplinkParseStatus.MissingData, "Uplink data is missing.");
        }

        byte[] payload;
        try
        {
            payload = Convert.FromBase64String(uplink.Data);
        }
        catch (FormatException ex)
        {
            return UplinkParseResult.Fail(UplinkParseStatus.InvalidBase64, ex.Message);
        }

        var best = SelectBestReception(uplink.RxInfo);

        var ctx = new UplinkContext
        {
            DevEui = NormalizeDevEui(devEui),
            Payload = payload,
            FrameCounter = uplink.FCnt,
            ReceivedAt = uplink.Time ?? DateTimeOffset.UtcNow,
            Rssi = best?.Rssi,
            Snr = best?.Snr,
        };

        return UplinkParseResult.Ok(ctx);
    }

    public static string NormalizeDevEui(string devEui) =>
        devEui.Trim().ToUpperInvariant();

    private static ChirpStackRxInfo? SelectBestReception(IEnumerable<ChirpStackRxInfo>? rxInfo)
    {
        if (rxInfo is null || rxInfo.Count() == 0)
        {
            return null;
        }

        ChirpStackRxInfo? best = null;
        foreach (var rx in rxInfo)
        {
            if (best is null || (rx.Rssi ?? double.MinValue) > (best.Rssi ?? double.MinValue))
            {
                best = rx;
            }
        }
        return best;
    }
}