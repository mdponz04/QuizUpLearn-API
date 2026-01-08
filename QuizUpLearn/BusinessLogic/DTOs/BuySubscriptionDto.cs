namespace BusinessLogic.DTOs
{
    public class BuySubscriptionDto
    {
        public Guid PlanId { get; set; }
        public string SuccessUrl { get; set; } = string.Empty;
        public string CanceledUrl { get; set; } = string.Empty;
    }
}
