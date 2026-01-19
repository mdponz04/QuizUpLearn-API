namespace BusinessLogic.DTOs.UserReportDtos
{
    public class ResponseUserReportDto
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public Guid? CommentId { get; set; }
        public string Reason { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }

        // Navigation properties
        public ResponseUserLiteDto? User { get; set; }
    }
}


