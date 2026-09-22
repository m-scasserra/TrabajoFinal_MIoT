using System.Reflection;
using Ingest.Application.Nodes;
using Ingest.Core.Config;
using Ingest.Infrastructure.Persistence;

namespace Ingest.Infrastructure.Tests.Persistence;

public class NodeRowMapperTests
{
    private const string ValidPulseConfig = """
        {
          "schema": 1,
          "mode": "Pulse",
          "config_version": 7,
          "config_hash": 50393,
          "phase_config":
          {
            "raw_bytes": 2,
            "three_phase": false,
            "phase_bits": 1,
            "active_phases": ["L1"]
          },
          "summary_mode": false,
          "pulse_counters": [ { "phase": "L1", "pulses_per_kwh": 1000} ]
        }
        """;

    private static NodeRow Row(string? configJson, string state = "ACTIVE") => new()
    {
        Id = Guid.NewGuid(),
        DevEui = "AABBCCDD",
        Alias = "node-test",
        OrgId = Guid.NewGuid(),
        OperativeState = state,
        ConfigJson = configJson,
    };

    [Fact]
    public void Map_ValidRow_ProducesCachedNode()
    {
        var result = NodeRowMapper.Map(Row(ValidPulseConfig));

        Assert.Equal(NodeMapStatus.Ok, result.Status);
        Assert.NotNull(result.Node);
        Assert.Equal(AcquisitionMode.Pulse, result.Node!.Config.Mode);
        Assert.Equal(7, result.Node.Config.ConfigVersion);
        Assert.Equal(NodeState.Active, result.Node.OperativeState);
    }

    [Fact]
    public void Map_ConfigNull_ReturnNoConfig()
    {
        var result = NodeRowMapper.Map(Row(configJson: null));

        Assert.Equal(NodeMapStatus.NoConfig, result.Status);
        Assert.Null(result.Node);
    }

    [Fact]
    public void Map_MalformedJson_ReturnsInvalidConfigJson()
    {
        var result = NodeRowMapper.Map(Row(configJson: "{ malformed json }"));

        Assert.Equal(NodeMapStatus.InvalidConfigJson, result.Status);
        Assert.Null(result.Node);
        Assert.NotNull(result.Error);
    }

    [Theory]
    [InlineData("ACTIVE", NodeState.Active)]
    [InlineData("INACTIVE", NodeState.Inactive)]
    [InlineData("MAINTENANCE", NodeState.Maintenance)]
    public void MapState_KnownValues(string dbValue, NodeState expected)
    {
        Assert.Equal(expected, NodeRowMapper.MapState(dbValue));
    }

    [Fact]
    public void MapState_UnknownValue_Throws()
    {
        Assert.Throws<InvalidOperationException>(() => NodeRowMapper.MapState("FROBNICATED"));
    }

    [Fact]
    public void Map_UnknownValue_Propagates()
    {
        Assert.Throws<InvalidOperationException>(() =>
            NodeRowMapper.Map(Row(ValidPulseConfig, state: "FROBNICATED")));
    }
}