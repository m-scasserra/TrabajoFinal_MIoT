using System.Text.Json;
using Ingest.Infrastructure.ChirpStack;

namespace Ingest.Infrastructure.Tests.ChirpStack;

public class UplinkContextFactoryTests
{
    private const string RealUplink = """
        {"deduplicationId":"1caa3b80-9a09-4b8e-a244-9c4bef5f169c",
        "time":"2023-02-11T11:59:27.902047+00:00",
        "deviceInfo":{"devEui":"a84041db2184d988","deviceName":"medidor-1"},
        "devAddr":"00000001","adr":true,"dr":5,"fCnt":5,"fPort":2,"confirmed":true,
        "data":"BYIIAlgeoKU=",
        "rxInfo":[{"gatewayId":"a84041ffff249aac","rssi":-44,"snr":13.8}],
        "txInfo":{"frequency":868300000}}
    """;

    private static ChirpStackUplink Parse(string json) =>
        JsonSerializer.Deserialize<ChirpStackUplink>(json)!;

    [Fact]
    public void Create_RealEvento_CreatesContext()
    {
        var result = UplinkContextFactory.Create(Parse(RealUplink));

        Assert.Equal(UplinkParseStatus.Ok, result.Status);
        var ctx = result.Context!;
        Assert.Equal("A84041DB2184D988", ctx.DevEui);
        Assert.Equal(5u, ctx.FrameCounter);
        Assert.Equal(-44, ctx.Rssi);
        Assert.Equal(13.8, ctx.Snr);
        Assert.Equal(new byte[] { 0x05, 0x82, 0x08, 0x02, 0x58, 0x1E, 0xA0, 0xA5 }, ctx.Payload);
    }

    [Fact]
    public void Create_MultipleGateways_PicksBestRssi()
    {
        const string json = """
            {"time":"2023-02-11T11:59:27Z","deviceInfo":{"devEui":"aabbccdd"},
            "fCnt":10,"data":"AA==",
            "rxInfo":[
              {"gatewayId":"gw1","rssi":-90,"snr":5.0},
              {"gatewayId":"gw2","rssi":-70,"snr":8.0},
              {"gatewayId":"gw3","rssi":-100,"snr":2.0}
            ]}
            """;

        var result = UplinkContextFactory.Create(Parse(json));

        Assert.Equal(UplinkParseStatus.Ok, result.Status);
        Assert.Equal(-70, result.Context!.Rssi);
        Assert.Equal(8.0, result.Context!.Snr);
    }

    [Fact]
    public void Create_NoDevEui_Fails()
    {
        const string json = """{"time":"2023-02-11T11:59:27Z","fCnt":10,"data":"AA=="}""";
        var result = UplinkContextFactory.Create(Parse(json));
        Assert.Equal(UplinkParseStatus.MissingDevEui, result.Status);
    }

    [Fact]
    public void Create_NoData_Fails()
    {
        const string json = """{"deviceInfo":{"devEui":"aabb"},"fCnt":10}""";
        var result = UplinkContextFactory.Create(Parse(json));
        Assert.Equal(UplinkParseStatus.MissingData, result.Status);
    }

    [Fact]
    public void Create_InvalidBase64_Fails()
    {
        const string json = """{"deviceInfo":{"devEui":"aabb"},"fCnt":1,"data":"not base64 !!!"}""";
        var result = UplinkContextFactory.Create(Parse(json));
        Assert.Equal(UplinkParseStatus.InvalidBase64, result.Status);
    }

    [Fact]
    public void Create_NoRxInfo_RssiAndSnrNull()
    {
        const string json = """{"deviceInfo":{"devEui":"aabb"},"fCnt":10,"data":"AA=="}""";
        var result = UplinkContextFactory.Create(Parse(json));
        Assert.Equal(UplinkParseStatus.Ok, result.Status);
        Assert.Null(result.Context!.Rssi);
        Assert.Null(result.Context!.Snr);
    }

    [Theory]
    [InlineData("a84041db2184d988", "A84041DB2184D988")]
    [InlineData("  AABBCCDD  ", "AABBCCDD")]
    public void NormalizeDevEui_TrimsAndUppercases(string input, string expected)
    {
        var result = UplinkContextFactory.NormalizeDevEui(input);
        Assert.Equal(expected, result);
    }
}