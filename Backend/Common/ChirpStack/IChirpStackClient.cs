namespace Backend.Common.ChirpStack;

public interface IChirpStackClient
{
    Task CreateGatewayAsync(string gatewayEui, string name, double? lat, double? lng, CancellationToken ct = default);
    Task<bool> GatewayExistsAsync(string gatewayEui, CancellationToken ct = default);
    Task DeleteGatewayAsync(string gatewayEui, CancellationToken ct = default);
    Task UpdateGatewayAsync(string gatewayEui, string name, double? lat, double? lng, CancellationToken ct = default);
    Task CreateDeviceAsync(string devEui, string joinEui, string name, string applicationId,
                           string deviceProfileId, string appKeyHex, CancellationToken ct = default);
    Task<bool> DeviceExistsAsync(string devEui, CancellationToken ct = default);
    Task UpdateDeviceAsync(string devEui, string name, string deviceProfileId, CancellationToken ct = default);
    Task DeleteDeviceAsync(string devEui, CancellationToken ct = default);
    Task<string> CreateDeviceProfileAsync(DeviceProfileData data, CancellationToken ct = default);
    Task UpdateDeviceProfileAsync(string chirpstackId, DeviceProfileData data, CancellationToken ct = default);
    Task DeleteDeviceProfileAsync(string chirpstackId, CancellationToken ct = default);
    Task<bool> DeviceProfileExistsAsync(string chirpstackId, CancellationToken ct = default);
}