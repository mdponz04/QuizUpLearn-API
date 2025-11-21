using Repository.Enums;

namespace BusinessLogic.DTOs.PaymentTransactionDtos
{
    public class ResponsePaymentTransactionDto
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public Guid SubscriptionPlanId { get; set; }
        public long Amount { get; set; }
        public DateTime? CompletedDate { get; set; }
        public TransactionStatusEnum Status { get; set; }
        public string? PaymentGatewayTransactionId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
