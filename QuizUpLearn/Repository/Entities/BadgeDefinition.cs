using Repository.Entities.BaseModelEntity;
using Repository.Enums;

namespace Repository.Entities
{
    public class BadgeDefinition : BaseEntity
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public BadgeDefinitionTypeEnum Type { get; set; }
        public int? TargetValue { get; set; }
        public int? MinQuizCount { get; set; }
        public int? MinAccuracy { get; set; }
    }
}
