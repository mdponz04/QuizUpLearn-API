using Repository.Entities.BaseModelEntity;
using Repository.Enums;

namespace Repository.Entities
{
    public class BadgeRule : BaseEntity
    {
        public Guid BadgeId { get; set; }
        public BadgeConditonEnum Condition { get; set; }
        public int RequiredValue { get; set; }
        public int? ToeicPart { get; set; }
        public virtual Badge? Badge { get; set; }
    }
}
