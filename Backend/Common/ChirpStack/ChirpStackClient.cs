using Chirpstack.Api;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.Extensions.Options;

namespace Backend.Common.ChirpStack;

public sealed class ChirpStackClient : IChirpStackClient, IDisposable
{
    private readonly GrpcChannel _channel;
    private readonly GatewayService.GatewayServiceClient _gateways;
    private readonly Metadata _auth;
    private readonly string _tenantId;

    public ChirpStackClient(IOptions<ChirpStackSettings> options)
    {
        var cfg = options.Value;
        _channel = GrpcChannel.ForAddress(cfg.GrpcAddress);
        _gateways = new GatewayService.GatewayServiceClient(_channel);
        _tenantId = cfg.TenantId;
        _auth = new Metadata { { "authorization", $"Bearer {cfg.ApiToken}" } };
    }

    public async Task CreateGatewayAsync(
        string gatewayEui, string name, double? lat, double? lng, CancellationToken ct = default)
    {
        var gw = new Gateway
        {
            GatewayId = gatewayEui.ToLowerInvariant(),
            Name = name,
            TenantId = _tenantId,
            StatsInterval = 30
        };

        if (lat.HasValue && lng.HasValue)
        {
            gw.Location = new Chirpstack.Common.Location
            {
                Latitude = lat.Value,
                Longitude = lng.Value,
                Source = Chirpstack.Common.LocationSource.Unknown;
            };
        }
        var req = new CreateGatewayRequest { Gateway = gw };
        await _gateways.CreateAsync(req, _auth, cancellationToken: ct);
    }

    public async Task<bool> GatewayExistsAsync(string gatewayEui, CancellationToken ct = default)
    {
        try
        {
            var req = new GetGatewayRequest { GatewayId = gatewayEui.ToLowerInvariant() };
            var resp = await _gateways.GetAsync(req, _auth, cancellationToken: ct);
            return resp?.Gateway is not null;
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.NotFound)
        {
            return false;
        }
    }

    public async Task DeleteGatewayAsync(string gatewayEui, CancellationToken ct = default)
    {
        try
        {
            var req = new DeleteGatewayRequest { GatewayId = gatewayEui.ToLowerInvariant() };
            await _gateways.DeleteAsync(req, _auth, cancellationToken: ct);
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.NotFound)
        {
        }
    }

    public void Dispose() => _channel.Dispose();
}