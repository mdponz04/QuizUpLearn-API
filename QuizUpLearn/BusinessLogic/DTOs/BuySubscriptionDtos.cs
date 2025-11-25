namespace BusinessLogic.DTOs
{
    public class BuySubscriptionRequestDtos
    {
        public Guid? userId { get; set; }
        public Guid planId { get; set; }
        public string? successUrl { get; set; }
        public string? cancelUrl { get; set; }
    }
}
