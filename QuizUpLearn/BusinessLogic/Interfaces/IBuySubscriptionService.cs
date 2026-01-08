using BusinessLogic.DTOs;

namespace BusinessLogic.Interfaces
{
    public interface IBuySubscriptionService
    {
        Task<(long, string)> StartSubscriptionPurchaseAsync(Guid userId, Guid planId, string successUrl, string canceledUrl);
        Task HandlePaymentSuccessAsync(long orderCode);
        Task HandlePaymentCancelAsync(long orderCode);
    }
}
