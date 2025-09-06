namespace GearQueue.Worker.Executor;

internal interface IJobExecutor
{
    void RegisterAsyncResultCallback(int connectionId, Func<string, JobResult, Task> callback);

    Task WaitAllExecutions();
}