namespace BusinessLogic.DTOs.SubscriptionDtos
{
    public class RequestSubscriptionDto
    {
        public Guid? UserId { get; set; }
        public Guid? SubscriptionPlanId { get; set; }
        public DateTime? EndDate { get; set; }
    }
}
