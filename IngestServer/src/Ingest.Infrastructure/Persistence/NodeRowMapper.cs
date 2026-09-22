using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using Ingest.Application.Nodes;
using Ingest.Core.Config;

namespace Ingest.Infrastructure.Persistence;

public sealed class NodeRow
{
    public required Guid Id { get; init; }
    public required string DevEui { get; init; }
    public required string Alias { get; init; }
    public required Guid? OrgId { get; init; }
    public required string OperativeState { get; init; }
    public required string? ConfigJson { get; init; }
}

public enum NodeMapStatus
{
    Ok,
    NoConfig,
    InvalidConfigJson
}

public readonly record struct NodeMapResult(NodeMapStatus Status, CachedNode? Node, string? Error)
{
    public static NodeMapResult Ok(CachedNode node) => new(NodeMapStatus.Ok, node, null);
    public static NodeMapResult NoConfig() => new(NodeMapStatus.NoConfig, null, null);
    public static NodeMapResult InvalidJson(string error) =>
        new(NodeMapStatus.InvalidConfigJson, null, error);
}

public static class NodeRowMapper
{
    public static NodeMapResult Map(NodeRow row)
    {
        if (row.ConfigJson is null)
        {
            return NodeMapResult.NoConfig();
        }

        NodeConfig? config;
        try
        {
            config = JsonSerializer.Deserialize<NodeConfig>(
                row.ConfigJson, NodeConfigSerialization.Options);
        }
        catch (JsonException ex)
        {
            return NodeMapResult.InvalidJson(ex.Message);
        }

        if (config is null)
        {
            return NodeMapResult.InvalidJson("Config deserialized to null");
        }

        var node = new CachedNode
        {
            Id = row.Id,
            DevEui = row.DevEui,
            Alias = row.Alias,
            OrgId = row.OrgId,
            OperativeState = MapState(row.OperativeState),
            Config = config
        };

        return NodeMapResult.Ok(node);
    }

    public static NodeState MapState(string dbValue) => dbValue switch
    {
        "ACTIVE" => NodeState.Active,
        "INACTIVE" => NodeState.Inactive,
        "MAINTENANCE" => NodeState.Maintenance,
        _ => throw new InvalidOperationException($"Unknown operative state: {dbValue}"),
    };
}