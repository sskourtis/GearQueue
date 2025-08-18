using GearQueue.Consumer;
using Microsoft.Extensions.Hosting;

namespace GearQueue.Extensions.Microsoft.DependencyInjection;

public class GearQueueHostedService(IEnumerable<IGearQueueConsumer> consumers) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.WhenAll(consumers.Select(c => c.StartConsuming(stoppingToken)));
    }
}