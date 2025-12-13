using BusinessLogic.DTOs.QuizDtos;

namespace BusinessLogic.DTOs.QuizGroupItemDtos
{
    public class ResponseQuizGroupItemDto
    {
        public Guid Id { get; set; }
        public string? Name { get; set; }
        public string? AudioUrl { get; set; }
        public string? ImageUrl { get; set; }
        public string? AudioScript { get; set; }
        public string? ImageDescription { get; set; }
        public string? PassageText { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public List<QuizResponseDto> Quizzes { get; set; } = new List<QuizResponseDto>();
    }
}
