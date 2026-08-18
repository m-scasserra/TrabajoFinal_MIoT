using Chirpstack.Api;
using Chirpstack.Common;
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
    private readonly DeviceService.DeviceServiceClient _devices;
    private readonly string _applicationId;
    private readonly DeviceProfileService.DeviceProfileServiceClient _deviceProfiles;

    public ChirpStackClient(IOptions<ChirpStackSettings> options)
    {
        var cfg = options.Value;
        _channel = GrpcChannel.ForAddress(cfg.GrpcAddress);
        _gateways = new GatewayService.GatewayServiceClient(_channel);
        _tenantId = cfg.TenantId;
        _auth = new Metadata { { "authorization", $"Bearer {cfg.ApiToken}" } };
        _devices = new DeviceService.DeviceServiceClient(_channel);
        _applicationId = cfg.ApplicationId;
        _deviceProfiles = new DeviceProfileService.DeviceProfileServiceClient(_channel);
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
                Source = Chirpstack.Common.LocationSource.Unknown
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

    public async Task UpdateGatewayAsync(string gatewayEui, string name, double? lat, double? lng, CancellationToken ct = default)
    {
        var gw = new Gateway
        {
            GatewayId = gatewayEui.ToLowerInvariant(),
            Name = name,
            TenantId = _tenantId,
            StatsInterval = 30,
            DownlinkPriority = 10
        };

        if (lat.HasValue && lng.HasValue)
        {
            gw.Location = new Chirpstack.Common.Location
            {
                Latitude = lat.Value,
                Longitude = lng.Value,
                Source = Chirpstack.Common.LocationSource.Unknown
            };
        }

        var req = new UpdateGatewayRequest { Gateway = gw };
        await _gateways.UpdateAsync(req, _auth, cancellationToken: ct);
    }

    public async Task CreateDeviceAsync(
        string devEui, string joinEui, string name, string applicationId,
        string deviceProfileId, string appKeyHex, CancellationToken ct = default)
    {
        devEui = devEui.ToLowerInvariant();

        var device = new Device
        {
            DevEui = devEui,
            JoinEui = joinEui.ToLowerInvariant(),
            Name = name,
            ApplicationId = applicationId,
            DeviceProfileId = deviceProfileId,
            IsDisabled = false
        };
        await _devices.CreateAsync(
            new CreateDeviceRequest { Device = device }, _auth, cancellationToken: ct);

        var keys = new DeviceKeys
        {
            DevEui = devEui,
            NwkKey = appKeyHex
        };
        await _devices.CreateKeysAsync(
            new CreateDeviceKeysRequest { DeviceKeys = keys }, _auth, cancellationToken: ct);
    }

    public async Task<bool> DeviceExistsAsync(string devEui, CancellationToken ct = default)
    {
        try
        {
            var req = new GetDeviceRequest { DevEui = devEui.ToLowerInvariant() };
            var resp = await _devices.GetAsync(req, _auth, cancellationToken: ct);
            return resp?.Device is not null;
        }
        catch (RpcException ex) when (
            ex.StatusCode == StatusCode.NotFound ||
            ex.StatusCode == StatusCode.Unauthenticated ||
            ex.StatusCode == StatusCode.PermissionDenied)
        {
            return false;
        }
    }

    public async Task UpdateDeviceAsync(string devEui, string name, string deviceProfileId, CancellationToken ct = default)
    {
        var device = new Device
        {
            DevEui = devEui.ToLowerInvariant(),
            Name = name,
            ApplicationId = _applicationId,
            DeviceProfileId = deviceProfileId,
            IsDisabled = false
        };
        await _devices.UpdateAsync(
            new UpdateDeviceRequest { Device = device }, _auth, cancellationToken: ct);
    }

    public async Task DeleteDeviceAsync(string devEui, CancellationToken ct = default)
    {
        try
        {
            await _devices.DeleteAsync(
                new DeleteDeviceRequest { DevEui = devEui.ToLowerInvariant() }, _auth, cancellationToken: ct);
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.NotFound)
        {
        }
    }

    public async Task<string> CreateDeviceProfileAsync(DeviceProfileData d, CancellationToken ct = default)
    {
        try
        {
            ProtoEnumMapper<Region>.FromOriginalName(d.Region);
            ProtoEnumMapper<MacVersion>.FromOriginalName(d.MacVersion);
            ProtoEnumMapper<RegParamsRevision>.FromOriginalName(d.RegParamsRevision);
        }
        catch (Exception ex)
        {
            throw new ArgumentException("Invalid enum value", ex);
        }

        var profile = new DeviceProfile
        {
            TenantId = _tenantId,
            Name = d.Name,
            Region = ProtoEnumMapper<Region>.FromOriginalName(d.Region),
            MacVersion = ProtoEnumMapper<MacVersion>.FromOriginalName(d.MacVersion),
            RegParamsRevision = ProtoEnumMapper<RegParamsRevision>.FromOriginalName(d.RegParamsRevision),
            AdrAlgorithmId = d.AdrAlgorithmId,
            UplinkInterval = (uint)d.UplinkInterval,
            DeviceStatusReqInterval = (uint)d.DeviceStatusReqInterval,
            SupportsOtaa = d.SupportsOtaa,
            FlushQueueOnActivate = d.FlushQueueOnActivate,
            AutoDetectMeasurements = d.AutoDetectMeasurements
        };

        if (!string.IsNullOrEmpty(d.RegionConfigId))
        {
            profile.RegionConfigId = d.RegionConfigId;
        }
        if (d.Ts003FPort.HasValue)
        {
            profile.AppLayerParams = new AppLayerParams
            {
                Ts003FPort = (uint)d.Ts003FPort.Value,
                Ts004FPort = (uint)(d.Ts004FPort ?? 0),
                Ts005FPort = (uint)(d.Ts005FPort ?? 0)
            };
        }

        var resp = await _deviceProfiles.CreateAsync(
            new CreateDeviceProfileRequest { DeviceProfile = profile }, _auth, cancellationToken: ct);

        return resp.Id;
    }

    public async Task UpdateDeviceProfileAsync(
        string chirpstackId, DeviceProfileData d, CancellationToken ct = default)
    {
        try
        {
            ProtoEnumMapper<Region>.FromOriginalName(d.Region);
            ProtoEnumMapper<MacVersion>.FromOriginalName(d.MacVersion);
            ProtoEnumMapper<RegParamsRevision>.FromOriginalName(d.RegParamsRevision);
        }
        catch (Exception ex)
        {
            throw new ArgumentException("Invalid enum value", ex);
        }

        var profile = new DeviceProfile
        {
            Id = chirpstackId,
            TenantId = _tenantId,
            Name = d.Name,
            Region = ProtoEnumMapper<Region>.FromOriginalName(d.Region),
            MacVersion = ProtoEnumMapper<MacVersion>.FromOriginalName(d.MacVersion),
            RegParamsRevision = ProtoEnumMapper<RegParamsRevision>.FromOriginalName(d.RegParamsRevision),
            AdrAlgorithmId = d.AdrAlgorithmId,
            UplinkInterval = (uint)d.UplinkInterval,
            DeviceStatusReqInterval = (uint)d.DeviceStatusReqInterval,
            SupportsOtaa = d.SupportsOtaa,
            FlushQueueOnActivate = d.FlushQueueOnActivate,
            AutoDetectMeasurements = d.AutoDetectMeasurements
        };

        if (!string.IsNullOrEmpty(d.RegionConfigId))
        {
            profile.RegionConfigId = d.RegionConfigId;
        }
        if (d.Ts003FPort.HasValue)
        {
            profile.AppLayerParams = new AppLayerParams
            {
                Ts003FPort = (uint)d.Ts003FPort.Value,
                Ts004FPort = (uint)(d.Ts004FPort ?? 0),
                Ts005FPort = (uint)(d.Ts005FPort ?? 0)
            };
        }

        await _deviceProfiles.UpdateAsync(
            new UpdateDeviceProfileRequest { DeviceProfile = profile }, _auth, cancellationToken: ct);
    }

    public async Task DeleteDeviceProfileAsync(string chirpstackId, CancellationToken ct = default)
    {
        try
        {
            await _deviceProfiles.DeleteAsync(
                new DeleteDeviceProfileRequest { Id = chirpstackId }, _auth, cancellationToken: ct);
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.NotFound) { }
    }

    public async Task<bool> DeviceProfileExistsAsync(string chirpstackId, CancellationToken ct = default)
    {
        try
        {
            var resp = await _deviceProfiles.GetAsync(
                new GetDeviceProfileRequest { Id = chirpstackId }, _auth, cancellationToken: ct);
            return resp?.DeviceProfile is not null;
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.NotFound)
        {
            return false;
        }
    }

    public void Dispose() => _channel.Dispose();
}

