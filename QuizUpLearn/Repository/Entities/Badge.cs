using Repository.Entities.BaseModelEntity;
using Repository.Enums;

namespace Repository.Entities
{
    public class Badge : BaseEntity
    {
        public string? Code { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public BadgeTypeEnum Type { get; set; }
        public virtual ICollection<BadgeRule>? BadgeRules { get; set; }
        public virtual ICollection<UserBadge>? UserBadges { get; set; }
    }
}
