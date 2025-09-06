using GearQueue.Worker;
using Microsoft.Extensions.Hosting;

namespace GearQueue.Extensions.Microsoft.DependencyInjection;

public class GearQueueHostedService(IEnumerable<IWorker> workers) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.WhenAll(workers.Select(c => c.Start(stoppingToken)));
    }
}