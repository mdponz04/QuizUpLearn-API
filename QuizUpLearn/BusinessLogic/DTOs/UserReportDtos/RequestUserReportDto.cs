namespace BusinessLogic.DTOs.UserReportDtos
{
    public class RequestUserReportDto
    {
        public Guid UserId { get; set; }
        public Guid? CommentId { get; set; }
        public string Reason { get; set; } = string.Empty;
    }
}


