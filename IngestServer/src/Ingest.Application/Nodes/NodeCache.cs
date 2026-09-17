using System.Collections.Concurrent;
using Ingest.Core.Config;
using Microsoft.Extensions.Logging;

namespace Ingest.Application.Nodes;

public sealed class NodeCache : INodeCache
{
    private readonly ConcurrentDictionary<string, CachedNode> _nodes = new();
    private readonly INodeRepository _repository;
    private readonly ILogger<NodeCache> _logger;

    public NodeCache(INodeRepository repository, ILogger<NodeCache> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<CachedNode?> GetAsync(string devEui, CancellationToken ct = default)
    {
        if (_nodes.TryGetValue(devEui, out var cached))
        {
            return cached;
        }

        var result = await LoadAsync(devEui, ct);

        if (result.Outcome == LoadOutcome.Loaded)
        {
            _nodes[devEui] = result.Node!;
            return result.Node;
        }

        return null;
    }
    public async Task InvalidateAsync(string devEui, CancellationToken ct = default)
    {
        if (!_nodes.ContainsKey(devEui))
        {
            _logger.LogDebug(
                "Node {DevEui} not found in cache before invalidation.", devEui);
            return;
        }

        var result = await LoadAsync(devEui, ct);

        switch (result.Outcome)
        {
            case LoadOutcome.Loaded:
                _nodes[devEui] = result.Node!;
                _logger.LogInformation("Node {DevEui} reloaded after invalidation.", devEui);
                break;

            case LoadOutcome.NotFound:
                _nodes.TryRemove(devEui, out _);
                _logger.LogInformation("Node {DevEui} removed from cache after invalidation.", devEui);
                break;

            case LoadOutcome.TransientError:
                _logger.LogWarning("Transient error occurred while reloading node {DevEui} after invalidation.", devEui);
                break;
        }
    }

    private async Task<LoadResult> LoadAsync(string devEui, CancellationToken ct = default)
    {
        CachedNode? node;
        try
        {
            node = await _repository.GetByDevEuiAsync(devEui, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load node {DevEui} from repository.", devEui);
            return LoadResult.TransientError;
        }

        if (node is null)
        {
            return LoadResult.NotFound;
        }

        var validation = NodeConfigValidator.Validate(node.Config);
        if (!validation.IsValid)
        {
            _logger.LogError(
                "Node {DevEui} (id {Id}) has invalid configuration. Errors:{NewLine}{ValidationErrors}",
                devEui, node.Id, Environment.NewLine, string.Join(", ", validation.Errors));
            return LoadResult.NotFound;
        }

        return LoadResult.Loaded(node);
    }

    private enum LoadOutcome { Loaded, NotFound, TransientError }

    private readonly record struct LoadResult(LoadOutcome Outcome, CachedNode? Node)
    {
        public static LoadResult Loaded(CachedNode node) => new(LoadOutcome.Loaded, node);
        public static readonly LoadResult NotFound = new(LoadOutcome.NotFound, null);
        public static readonly LoadResult TransientError = new(LoadOutcome.TransientError, null);
    }
}