using Net.payOS.Types;

namespace BusinessLogic.Interfaces
{
    public interface IPaymentService
    {
        public Task<CreatePaymentResult?> CreatePaymentLinkAsync(int amount, string description, List<ItemData> items, string? successUrl, string? cancelUrl);
        public Task<PaymentLinkInformation?> CancelPaymentLinkAsync(long orderCode, string? reason = null);
        public Task<PaymentLinkInformation?> GetPaymentInfoAsync(long orderCode);
    }
}
