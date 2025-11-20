namespace BusinessLogic.DTOs.PaymentDtos
{
    public class PaymentLink
    {
        public long PaymentLinkId { get; set; }
        public long OrderCode { get; set; }
        public int Amount { get; set; }
        public string? CheckoutUrl { get; set; }
        public string? Status { get; set; }
        public List<Transaction>? Transactions { get; set; }
    }

    public class Transaction
    {
        public string? Id { get; set; }
        public int Amount { get; set; }
        public string? Status { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class ConfirmWebhookResponse
    {
        public string? Url { get; set; }
        public string? Status { get; set; }
    }
    public class PaymentLinkItem
    {
        public string Name { get; set; } = null!;
        public int Quantity { get; set; } = 1;
        public int Price { get; set; }
        public string? Description { get; set; }
    }

}
