using Ingest.Application.Nodes;

namespace Ingest.Application.Tests.Nodes;

public sealed class FakeNodeRepository : INodeRepository
{
    private readonly Dictionary<string, CachedNode> _nodes = new();
    private readonly HashSet<string> _throwOn = new();

    public int CallCount { get; private set; }

    public void Set(CachedNode node) => _nodes[node.DevEui] = node;

    public void Remove(string devEui) => _nodes.Remove(devEui);

    public void FailOn(string devEui) => _throwOn.Add(devEui);

    public void StopFailing(string devEui) => _throwOn.Remove(devEui);

    public Task<CachedNode?> GetByDevEuiAsync(string devEui, CancellationToken ct = default)
    {
        CallCount++;

        if (_throwOn.Contains(devEui))
        {
            throw new InvalidOperationException($"Failed on devEui: {devEui}");
        }

        _nodes.TryGetValue(devEui, out var node);
        return Task.FromResult(node);
    }
}