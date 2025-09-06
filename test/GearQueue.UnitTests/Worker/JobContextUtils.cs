using System.Text;
using GearQueue.Protocol;
using GearQueue.Protocol.Response;
using GearQueue.UnitTests.Utils;
using GearQueue.Worker;
using GearQueue.Worker.Executor;

namespace GearQueue.UnitTests.Worker;

public static class JobContextUtils
{
    public static JobAssign CreateJobAssign(string? functionName = null)
    {
        var handle = $"job_handle_{RandomData.GetString(5)}";
        functionName ??= $"function_name_{RandomData.GetString(5)}";
        var payload = RandomData.GetRandomBytes(25);
        
        var packetData = Encoding.UTF8.GetBytes($"{handle}\0{functionName}\0").ToList();
        packetData.AddRange(payload);

        return JobAssign.Create(packetData.ToArray());
    }
    
    public static JobAssign CreateJobAssignWithKey(string? functionName, string key)
    {
        var handle = $"job_handle_{RandomData.GetString(5)}";
        var uniqueId = UniqueId.Create(RandomData.GetString(2), key);
        var payload = RandomData.GetRandomBytes(25);
        
        var packetData = Encoding.UTF8.GetBytes($"{handle}\0{functionName}\0{uniqueId}\0").ToList();
        packetData.AddRange(payload);

        return JobAssign.CreateUniq(packetData.ToArray());
    }
    
    public  static JobContext CreateJobContext(int connectionId = 1)
    {
        return new JobContext(CreateJobAssign(), null, connectionId, CancellationToken.None);
    }

    public static JobContext CreateBatchJobContext(int batchSize = 5, int connectionId = 1)
    {
        var jobs = Enumerable.Range(0, batchSize).Select(_ => (connectionId, CreateJobAssign()));
        
        return new JobContext(jobs.ToList(), null, null, CancellationToken.None);
    }

    internal static List<(string Handle, JobResult Result)> RegisterResults(this IJobExecutor executor, int connectionId)
    {
        var receivedResults = new List<(string Handle, JobResult Result)>();
        executor.RegisterAsyncResultCallback(connectionId, (handle, jobResult) =>
        {
            receivedResults.Add((handle, jobResult));;
            return Task.CompletedTask;
        });

        return receivedResults;
    }
}