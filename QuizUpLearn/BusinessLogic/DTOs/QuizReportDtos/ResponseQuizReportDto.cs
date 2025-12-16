using BusinessLogic.DTOs.QuizDtos;

namespace BusinessLogic.DTOs.QuizReportDtos
{
    public class ResponseQuizReportDto
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public Guid QuizId { get; set; }
        public string Description { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? DeletedAt { get; set; }

        // Navigation properties
        public ResponseUserDto? User { get; set; }
        public QuizResponseDto? Quiz { get; set; }
    }
}