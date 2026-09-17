namespace Ingest.Application.Nodes;

public interface INodeCache
{
    Task<CachedNode?> GetAsync(string devEui, CancellationToken ct = default);
    Task InvalidateAsync(string devEui, CancellationToken ct = default);
}