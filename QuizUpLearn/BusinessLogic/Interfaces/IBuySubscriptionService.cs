using BusinessLogic.DTOs;

namespace BusinessLogic.Interfaces
{
    public interface IBuySubscriptionService
    {
        Task<(long, string)> StartSubscriptionPurchaseAsync(BuySubscriptionRequestDtos dto);
        Task HandlePaymentSuccessAsync(long orderCode);
        Task HandlePaymentCancelAsync(long orderCode);
    }
}
