using Repository.Entities.BaseModelEntity;

namespace Repository.Entities
{
    public class Role : BaseEntity
    {
        public required string RoleName { get; set; }
        public required string DisplayName { get; set; }
        public string? Description { get; set; }
        public string Permissions { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;

        // Navigation
        public virtual ICollection<Account> Accounts { get; set; } = new List<Account>();
    }
}
