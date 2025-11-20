namespace BusinessLogic.Interfaces
{
    public interface IBuySubscriptionService
    {
        Task<(long, string)> StartSubscriptionPurchaseAsync(Guid userId, Guid planId);
        Task HandlePaymentSuccessAsync(long orderCode);
        Task HandlePaymentCancelAsync(long orderCode);
    }
}
