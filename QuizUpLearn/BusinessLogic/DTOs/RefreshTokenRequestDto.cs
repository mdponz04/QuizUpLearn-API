namespace BusinessLogic.DTOs
{
    public class RefreshTokenRequestDto
    {
        public required Guid AccountId { get; set; }
        public required string RefreshToken { get; set; }
    }
}

