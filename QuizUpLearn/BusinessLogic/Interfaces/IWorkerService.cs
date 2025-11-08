namespace BusinessLogic.Interfaces
{
    public interface IWorkerService
    {
        Task EnqueueJob(Func<IServiceProvider, CancellationToken, Task> job);
    }
}
