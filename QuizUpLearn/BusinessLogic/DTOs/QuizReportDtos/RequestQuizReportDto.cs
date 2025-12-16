namespace BusinessLogic.DTOs.QuizReportDtos
{
    public class RequestQuizReportDto
    {
        public Guid UserId { get; set; } = Guid.Empty;
        public Guid QuizId { get; set; }
        public string Description { get; set; } = string.Empty;
    }
}