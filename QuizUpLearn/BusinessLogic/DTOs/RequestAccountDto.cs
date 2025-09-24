namespace BusinessLogic.DTOs
{
    public class RequestAccountDto
    {
        public required string Email { get; set; }
        public required string PasswordHash { get; set; }
    }
}


