namespace Ingest.Application.Nodes;

public interface INodeRepository
{
    Task<CachedNode?> GetByDevEuiAsync(string devEui, CancellationToken ct = default);
}