using GearQueue.Consumer;
using Microsoft.Extensions.Hosting;

namespace GearQueue.Extensions.Microsoft.DependencyInjection;

public class GearQueueHostedService<T>(IGearQueueConsumer consumer) : BackgroundService where T : IGearQueueBaseHandler
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await consumer.StartConsuming(stoppingToken);
    }
}