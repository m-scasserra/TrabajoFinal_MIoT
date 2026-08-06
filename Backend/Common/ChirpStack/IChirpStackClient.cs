namespace Backend.Common.ChirpStack;

public interface IChirpStackClient
{
    Task CreateGatewayAsync(string gatewayEui, string name, double? lat, double? lng, CancellationToken ct = default);
    Task<bool> GatewayExistsAsync(string gatewayEui, CancellationToken ct = default);
    Task DeleteGatewayAsync(string gatewayEui, CancellationToken ct = default);
}