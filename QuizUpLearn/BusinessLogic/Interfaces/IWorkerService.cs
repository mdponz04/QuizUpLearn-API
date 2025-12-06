namespace BusinessLogic.Interfaces
{
    public interface IWorkerService
    {
        Task EnqueueJob(Func<IServiceProvider, CancellationToken, Task> job);
        void RegisterActiveJob(Guid userId, Guid jobId, Guid quizSetId);
        void RemoveActiveJob(Guid jobId);
        (Guid jobId, Guid quizSetId)? GetActiveJobForUser(Guid userId);
    }
}
