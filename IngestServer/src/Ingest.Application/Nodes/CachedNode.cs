using Ingest.Core.Config;

namespace Ingest.Application.Nodes;

public sealed class CachedNode
{
    public required Guid Id { get; init; }
    public required string DevEui { get; init; }
    public required string Alias { get; init; }
    public Guid? OrgId { get; init; }
    public required NodeState OperativeState { get; init; }
    public required NodeConfig Config { get; init; }
}