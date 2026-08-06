using Backend.Common.Security;
using Backend.Features.Gateways.Dtos;

namespace Backend.Features.Gateways;

public interface IGatewayService
{
    Task<IEnumerable<GatewayDto>> ListAsync(CurrentUser me);
    Task<GatewayDto?> GetByEuiAsync(CurrentUser me, string eui);
    Task<GatewayDto> CreateAsync(CurrentUser me, CreateGatewayRequest req, CancellationToken ct = default);
    Task<int> ReconcilePendingAsync(CancellationToken ct = default);
}