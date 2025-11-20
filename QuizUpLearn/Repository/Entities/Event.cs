using Repository.Entities.BaseModelEntity;

namespace Repository.Entities
{
    public class Event : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public long MaxParticipants { get; set; }
        public string Status { get; set; } = string.Empty;

        public Guid CreatedBy { get; set; }
        // Navigation property
        public virtual User? Creator { get; set; }
    }
}
