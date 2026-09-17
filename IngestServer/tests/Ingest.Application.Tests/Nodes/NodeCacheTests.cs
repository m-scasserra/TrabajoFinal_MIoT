using Ingest.Application.Nodes;
using Ingest.Core.Config;
using Ingest.Core.Protocol;
using Microsoft.Extensions.Logging.Abstractions;

namespace Ingest.Application.Tests.Nodes;

public class NodeCacheTests
{
    private static CachedNode ValidNode(string devEui, byte configVersion = 7) => new()
    {
        Id = Guid.NewGuid(),
        DevEui = devEui,
        Alias = $"node-{devEui}",
        OrgId = Guid.NewGuid(),
        OperativeState = NodeState.Active,
        Config = new NodeConfig
        {
            Schema = 1,
            Mode = AcquisitionMode.Pulse,
            ConfigVersion = configVersion,
            PhaseConfig = new PhaseConfig
            {
                RawByte = 0x02,
                ThreePhase = false,
                PhaseBits = 1,
                ActivePhases = [PhaseTag.L1],
            },
            PulseCounters = [new PulseCounter { Phase = PhaseTag.L1, PulsesPerKwh = 1000 }]
        },
    };

    private static CachedNode InvalidConfigNode(string devEui) => new()
    {
        Id = Guid.NewGuid(),
        DevEui = devEui,
        Alias = "roto",
        OrgId = Guid.NewGuid(),
        OperativeState = NodeState.Active,
        Config = new NodeConfig
        {
            Schema = 1,
            Mode = AcquisitionMode.Modbus,
            ConfigVersion = 1,
            PhaseConfig = new PhaseConfig
            {
                RawByte = 0x02,
                ThreePhase = false,
                PhaseBits = 1,
                ActivePhases = [PhaseTag.L1],
            },
        },
    };

    private static NodeCache MakeCache(FakeNodeRepository repo) =>
        new(repo, NullLogger<NodeCache>.Instance);

    [Fact]
    public async Task Get_ValidNode_ReturnsAndAddsToCache()
    {
        var repo = new FakeNodeRepository();
        repo.Set(ValidNode("AABBCCDD"));
        var cache = MakeCache(repo);

        var first = await cache.GetAsync("AABBCCDD");
        var second = await cache.GetAsync("AABBCCDD");

        Assert.NotNull(first);
        Assert.NotNull(second);
        Assert.Equal(1, repo.CallCount);
    }

    [Fact]
    public async Task Get_NonExistentNode_ReturnsNull()
    {
        var repo = new FakeNodeRepository();
        var cache = MakeCache(repo);

        var result = await cache.GetAsync("AABBCCDD");

        Assert.Null(result);
    }

    [Fact]
    public async Task Get_InvalidNode_ReturnsNullAndNoCache()
    {
        var repo = new FakeNodeRepository();
        repo.Set(InvalidConfigNode("BADCFG00"));
        var cache = MakeCache(repo);

        var first = await cache.GetAsync("BADCFG00");
        var second = await cache.GetAsync("BADCFG00");

        Assert.Null(first);
        Assert.Null(second);
        Assert.Equal(2, repo.CallCount);
    }

    [Fact]
    public async Task Get_TransientError_ReturnsNullAndNoCache()
    {
        var repo = new FakeNodeRepository();
        repo.Set(ValidNode("DBDOWN00"));
        repo.FailOn("DBDOWN00");
        var cache = MakeCache(repo);

        var result = await cache.GetAsync("DBDOWN00");
        Assert.Null(result);

        repo.StopFailing("DBDOWN00");
        var recovered = await cache.GetAsync("DBDOWN00");
        Assert.NotNull(recovered);
    }

    [Fact]
    public async Task Invalidate_NodeOnCache_ReloadsNewConfig()
    {
        var repo = new FakeNodeRepository();
        repo.Set(ValidNode("RELOAD00", configVersion: 7));
        var cache = MakeCache(repo);

        var before = await cache.GetAsync("RELOAD00");
        Assert.Equal(7, before!.Config.ConfigVersion);

        repo.Set(ValidNode("RELOAD00", configVersion: 8));
        await cache.InvalidateAsync("RELOAD00");

        var after = await cache.GetAsync("RELOAD00");
        Assert.Equal(8, after!.Config.ConfigVersion);
    }

    [Fact]
    public async Task Invalidate_NodeNotOnCache_DoesNothing()
    {
        var repo = new FakeNodeRepository();
        repo.Set(ValidNode("NOTCACHD"));
        var cache = MakeCache(repo);

        await cache.InvalidateAsync("NOTCACHD");

        Assert.Equal(0, repo.CallCount);
    }

    [Fact]
    public async Task Invalidate_TransientError_KeepsOldConfig()
    {
        var repo = new FakeNodeRepository();
        repo.Set(ValidNode("KEEPOLD0", configVersion: 7));
        var cache = MakeCache(repo);

        await cache.GetAsync("KEEPOLD0");

        repo.FailOn("KEEPOLD0");
        await cache.InvalidateAsync("KEEPOLD0");

        repo.StopFailing("KEEPOLD0");
        var still = await cache.GetAsync("KEEPOLD0");
        Assert.NotNull(still);
        Assert.Equal(7, still!.Config.ConfigVersion);
    }

    [Fact]
    public async Task Invalidate_NodeDeleted_RemovesFromCache()
    {
        var repo = new FakeNodeRepository();
        repo.Set(ValidNode("GONE0000"));
        var cache = MakeCache(repo);

        await cache.GetAsync("GONE0000");

        repo.Remove("GONE0000");
        await cache.InvalidateAsync("GONE0000");

        var result = await cache.GetAsync("GONE0000");
        Assert.Null(result);
    }
}