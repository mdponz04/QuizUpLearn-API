namespace BusinessLogic.DTOs
{
    public class RequestUserDto
    {
        public required string Username { get; set; }
        public string FullName { get; set; } = string.Empty;
        public required string AvatarUrl { get; set; }
        public string? Bio { get; set; }
        public string? PreferredLanguage { get; set; }
        public string? Timezone { get; set; }
    }
}
