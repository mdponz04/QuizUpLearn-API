namespace BusinessLogic.DTOs.SubscriptionPlanDtos
{
    public class RequestSubscriptionPlanDto
    {
        public string? Name { get; set; }
        public long Price { get; set; }
        public int DurationDays { get; set; }
        public bool CanAccessPremiumContent { get; set; }
        public bool CanAccessAiFeatures { get; set; }
        public int AiGenerateQuizSetMaxTimes { get; set; }
        public bool IsActive { get; set; }
    }
}
