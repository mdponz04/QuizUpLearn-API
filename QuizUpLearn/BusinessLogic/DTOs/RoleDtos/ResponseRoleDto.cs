namespace BusinessLogic.DTOs.RoleDtos
{
    public class ResponseRoleDto
    {
        public Guid Id { get; set; }
        public string? RoleName { get; set; }
        public string? DisplayName { get; set; }
        public string? Description { get; set; }
        public string? Permissions { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? DeletedAt { get; set; }
    }
}
