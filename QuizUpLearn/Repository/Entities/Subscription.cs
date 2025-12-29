using Repository.Entities.BaseModelEntity;

namespace Repository.Entities
{
    public class Subscription : BaseEntity
    {
        public Guid UserId { get; set; }
        public Guid SubscriptionPlanId { get; set; }
        public DateTime? EndDate { get; set; }
        public virtual User? User { get; set; }
        public virtual SubscriptionPlan? SubscriptionPlan { get; set; }
    }
}
