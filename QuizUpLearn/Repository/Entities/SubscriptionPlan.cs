using Repository.Entities.BaseModelEntity;

namespace Repository.Entities
{
    public class SubscriptionPlan : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public long Price { get; set; } = 0; //in vnd
        public int DurationDays { get; set; }
        public bool CanAccessPremiumContent { get; set; } = false;
        public bool CanAccessAiFeatures { get; set; } = false;
        public bool IsActive { get; set; } = true;
        public bool IsBuyable { get; set; } = true;
    }
}
