using Backend.Features.Gateways;
using Backend.Features.Nodes;
using Backend.Features.DeviceProfiles;

namespace Backend.Common.Sync;

public sealed class ChirpStackSyncWorker(
    IServiceProvider services,
    ILogger<ChirpStackSyncWorker> logger) : BackgroundService
{
    private readonly TimeSpan _interval = TimeSpan.FromMinutes(5);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using (var scope = services.CreateScope())
            {
                var sp = scope.ServiceProvider;

                await ReconcileSafely(
                    () => sp.GetRequiredService<IDeviceProfileService>().ReconcilePendingAsync(),
                    "device profiles", stoppingToken);

                await ReconcileSafely(
                    () => sp.GetRequiredService<IGatewayService>().ReconcilePendingAsync(),
                    "gateways", stoppingToken);

                await ReconcileSafely(
                    () => sp.GetRequiredService<INodeService>().ReconcilePendingAsync(),
                    "nodes", stoppingToken);
            }

            try
            {
                await Task.Delay(_interval, stoppingToken);
            }
            catch (TaskCanceledException)
            {
                break;
            }
        }
    }

    private async Task ReconcileSafely(
        Func<Task<int>> reconcile, string label, CancellationToken ct)
    {
        if (ct.IsCancellationRequested)
            return;

        try
        {
            await reconcile();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during {Label} sync worker execution.", label);
        }
    }
}