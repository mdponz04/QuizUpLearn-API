namespace BusinessLogic.DTOs
{
    public class RequestRoleDto
    {
        public required string RoleName { get; set; }
        public required string DisplayName { get; set; }
        public string? Description { get; set; }
        public string? Permissions { get; set; }
        public bool IsActive { get; set; }
    }
}
