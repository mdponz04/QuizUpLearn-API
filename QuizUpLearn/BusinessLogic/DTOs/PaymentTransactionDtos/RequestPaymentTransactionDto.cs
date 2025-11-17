namespace BusinessLogic.DTOs.PaymentTransactionDtos
{
    public class RequestPaymentTransactionDto
    {
        public Guid UserId { get; set; }
        public Guid SubscriptionPlanId { get; set; }
        public long Amount { get; set; }
        public string? PaymentGatewayTransactionId { get; set; }
    }
}
