namespace BusinessLogic.Interfaces
{
    public interface ISubscriptionUsageService
    {
        Task ResetUsageForFreeSubscriptions(int resetDay);
    }
}
