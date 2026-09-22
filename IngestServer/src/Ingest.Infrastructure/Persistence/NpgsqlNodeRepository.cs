using Ingest.Application.Nodes;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Ingest.Infrastructure.Persistence;

public sealed class NpgsqlNodeRepository : INodeRepository
{
    private const string Query = """
        SELECT id, dev_eui, alias, org_id, operative_state, config
        FROM "general".nodes
        WHERE dev_eui = @dev_eui
        LIMIT 1;
        """;

    private readonly NpgsqlDataSource _dataSource;
    private readonly ILogger<NpgsqlNodeRepository> _logger;

    public NpgsqlNodeRepository(NpgsqlDataSource dataSource, ILogger<NpgsqlNodeRepository> logger)
    {
        _dataSource = dataSource;
        _logger = logger;
    }

    public async Task<CachedNode?> GetByDevEuiAsync(string devEui, CancellationToken ct = default)
    {
        var row = await ReadRowAsync(devEui, ct);
        if (row is null)
        {
            return null;
        }

        var result = NodeRowMapper.Map(row);
        switch (result.Status)
        {
            case NodeMapStatus.Ok:
                return result.Node;

            case NodeMapStatus.NoConfig:
                _logger.LogWarning("Node with dev_eui {DevEui} has no config", devEui);
                return null;

            case NodeMapStatus.InvalidConfigJson:
                _logger.LogError("Node with dev_eui {DevEui} has invalid config: {Error}", devEui, result.Error);
                return null;

            default:
                return null;
        }
    }

    private async Task<NodeRow?> ReadRowAsync(string devEui, CancellationToken ct = default)
    {
        await using var cmd = _dataSource.CreateCommand(Query);
        cmd.Parameters.AddWithValue("dev_eui", devEui);

        await using var reader = await cmd.ExecuteReaderAsync(ct);

        if (!await reader.ReadAsync(ct))
        {
            return null;
        }

        int orgIdOrdinal = reader.GetOrdinal("org_id");
        int configOrdinal = reader.GetOrdinal("config");

        return new NodeRow
        {
            Id = reader.GetGuid(reader.GetOrdinal("id")),
            DevEui = reader.GetString(reader.GetOrdinal("dev_eui")),
            Alias = reader.GetString(reader.GetOrdinal("alias")),
            OrgId = await reader.IsDBNullAsync(orgIdOrdinal, ct)
                ? null
                : reader.GetGuid(orgIdOrdinal),
            OperativeState = reader.GetString(reader.GetOrdinal("operative_state")),
            ConfigJson = await reader.IsDBNullAsync(configOrdinal, ct)
                ? null
                : reader.GetString(configOrdinal),
        };
    }
}