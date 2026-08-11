using Backend.Common.Security;
using Backend.Features.Nodes.Dtos;

namespace Backend.Features.Nodes;

public interface INodeService
{
    Task<IEnumerable<NodeDto>> ListAsync(CurrentUser me);
    Task<NodeDto?> GetByEuiAsync(CurrentUser me, string devEui);
    Task<NodeDto> ActivateAsync(CurrentUser me, ActivateNodeRequest req, CancellationToken ct = default);
    Task<bool> DeleteAsync(CurrentUser me, Guid id, CancellationToken ct = default);
    Task<int> ReconcilePendingAsync(CancellationToken ct = default);
    Task<ProvisionNodeResponse> ProvisionAsync(ProvisionNodeRequest req);
    Task<NodeDto?> UpdateProvisionAsync(Guid id, UpdateProvisionRequest req);
    Task<string> RotateAppKeyAsync(Guid id);
}