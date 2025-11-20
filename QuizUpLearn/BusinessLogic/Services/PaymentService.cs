using BusinessLogic.Interfaces;
using Microsoft.Extensions.Configuration;
using Net.payOS;
using Net.payOS.Types;

namespace BusinessLogic.Services
{
    public class PaymentService : IPaymentService
    {
        private readonly PayOS _payOS;
        private readonly string clientId;
        private readonly string apiKey;
        private readonly string checksumKey;

        public PaymentService(IConfiguration configuration)
        {
            clientId = configuration["PayOS:ClientId"] ?? throw new ArgumentNullException("Payos client id is not configured.");
            apiKey = configuration["PayOS:ApiKey"] ?? throw new ArgumentNullException("Payos api key is not configured.");
            checksumKey = configuration["PayOS:ChecksumKey"] ?? throw new ArgumentNullException("Payos checksum key is not configured.");

            _payOS = new PayOS(clientId, apiKey, checksumKey);
        }

        public async Task<CreatePaymentResult?> CreatePaymentLinkAsync(int amount, string description, List<ItemData> items)
        {
            items = items ?? new List<ItemData>();

            CreatePaymentResult? paymentLink;
            try
            {
                var paymentData = new PaymentData(
                    DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                    amount,
                    description,
                    items,
                    "/payment/success",
                    "/payment/cancel"
                );

                paymentLink = await _payOS.createPaymentLink(paymentData);
            }
            catch(Exception ex)
            {
                throw new Exception("Error creating payment link ", ex);
            }

            return paymentLink;
        }
        public async Task<PaymentLinkInformation?> CancelPaymentLinkAsync(long orderCode, string? reason = null)
        {
            if (reason != null)
                return await _payOS.cancelPaymentLink(orderCode, reason);

            return await _payOS.cancelPaymentLink(orderCode);
        }
        public async Task<PaymentLinkInformation?> GetPaymentInfoAsync(long orderCode)
        {
            return await _payOS.getPaymentLinkInformation(orderCode);
        }
        public async Task<string> ConfirmWebhookUrl(string webhookUrl)
        {
            return await _payOS.confirmWebhook(webhookUrl);
        }
    }
}
