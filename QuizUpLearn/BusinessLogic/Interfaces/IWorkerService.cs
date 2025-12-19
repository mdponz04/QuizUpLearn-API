namespace BusinessLogic.Interfaces
{
    public interface IWorkerService
    {
        Task EnqueueJob(Func<IServiceProvider, CancellationToken, Task> job);
        void RegisterActiveJob(Guid userId, Guid jobId);
        void RemoveActiveJob(Guid jobId);
        Guid? GetActiveJobForUser(Guid userId);
    }
}
