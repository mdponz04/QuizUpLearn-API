namespace BusinessLogic.DTOs.SubscriptionPlanDtos
{
    public class ResponseSubscriptionPlanDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public long Price { get; set; }
        public int DurationDays { get; set; }
        public bool CanAccessPremiumContent { get; set; }
        public bool CanAccessAiFeatures { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? DeletedAt { get; set; }
    }
}
