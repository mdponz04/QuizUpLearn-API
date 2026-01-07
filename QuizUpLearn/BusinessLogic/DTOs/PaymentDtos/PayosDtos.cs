namespace BusinessLogic.DTOs.PaymentDtos
{
    public class PayosWebhookDto
    {
        public string Code { get; set; } = default!;
        public string Desc { get; set; } = default!;
        public bool Success { get; set; }
        public PayosWebhookDataDto Data { get; set; } = default!;
        public string Signature { get; set; } = default!;
    }
    public class PayosWebhookDataDto
    {
        public long OrderCode { get; set; }
        public int Amount { get; set; }
        public string Description { get; set; } = default!;
        public string PaymentLinkId { get; set; } = default!;
        public string TransactionDateTime { get; set; } = default!;
    }
}
