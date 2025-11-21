namespace BusinessLogic.DTOs
{
    public class ResponseAccountDto
    {
        public Guid Id { get; set; }
        public string? Email { get; set; }
        public string Username { get; set; } = string.Empty;
        public Guid UserId { get; set; }
        public Guid RoleId { get; set; }
        public bool IsEmailVerified { get; set; }
        public DateTime? LastLoginAt { get; set; }
        public int LoginAttempts { get; set; }
        public DateTime? LockoutUntil { get; set; }
        public bool IsActive { get; set; }
        public bool IsBanned { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? DeletedAt { get; set; }
    }
}


