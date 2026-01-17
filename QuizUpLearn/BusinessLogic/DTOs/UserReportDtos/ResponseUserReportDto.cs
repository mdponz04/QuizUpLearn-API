using BusinessLogic.DTOs;

namespace BusinessLogic.DTOs.UserReportDtos
{
    public class ResponseUserReportDto
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string Reason { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? DeletedAt { get; set; }

        // Navigation properties
        public ResponseUserDto? User { get; set; }
    }
}


