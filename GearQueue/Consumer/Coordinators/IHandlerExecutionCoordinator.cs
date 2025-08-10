using GearQueue.Protocol.Response;

namespace GearQueue.Consumer.Coordinators;

internal interface IHandlerExecutionCoordinator
{
    /// <summary>
    /// Register a result callback for async job execution
    /// </summary>
    /// <param name="connectionId"></param>
    /// <param name="callback"></param>
    void RegisterAsyncResultCallback(int connectionId, Func<string, JobStatus, Task> callback);
    
    /// <summary>
    /// Accepts a job and arranges the execution of the job. Depending on the implementation, the job may be
    /// executed synchronously or asynchronously.
    /// </summary>
    /// <param name="connectionId"></param>
    /// <param name="job"></param>
    /// <param name="cancellationToken"></param>
    /// <returns>Returns the job status when it is executed synchronously, otherwise it returns null</returns>
    Task<JobStatus?> ArrangeExecution(int connectionId, JobAssign job, CancellationToken cancellationToken);
    
    Task WaitAllExecutions();
}