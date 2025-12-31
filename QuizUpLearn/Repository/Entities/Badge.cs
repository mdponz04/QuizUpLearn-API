using Repository.Entities.BaseModelEntity;

namespace Repository.Entities
{
    public class Badge : BaseEntity
    {
        public Guid UserId { get; set; }
        public Guid? TournamentId { get; set; }
        public Guid? EventId { get; set; }
        public Guid BadgeDefinitionId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;

        public virtual User? User { get; set; }
        public virtual Tournament? Tournament { get; set; }
        public virtual Event? Event { get; set; }
        public virtual BadgeDefinition? BadgeDefinition { get; set; }
    }
}
