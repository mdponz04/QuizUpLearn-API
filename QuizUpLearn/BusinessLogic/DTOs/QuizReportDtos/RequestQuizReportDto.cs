namespace BusinessLogic.DTOs.QuizReportDtos
{
    public class RequestQuizReportDto
    {
        public Guid UserId { get; set; }
        public Guid QuizId { get; set; }
        public string Description { get; set; } = string.Empty;
    }
}