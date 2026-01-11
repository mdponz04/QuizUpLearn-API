using System.ComponentModel.DataAnnotations;

namespace BusinessLogic.DTOs.SubscriptionPlanDtos
{
    public class RequestSubscriptionPlanDto
    {
        public string? Name { get; set; }
        [Range(1, int.MaxValue, ErrorMessage = "Price must be greater than 0")]
        public long Price { get; set; }
        [Range(1, int.MaxValue, ErrorMessage = "DurationDays must be greater than 0")]
        public int DurationDays { get; set; }
        public bool CanAccessPremiumContent { get; set; }
        public bool CanAccessAiFeatures { get; set; }
        public bool IsActive { get; set; }
        public bool IsBuyable { get; set; } = true;
    }
}
