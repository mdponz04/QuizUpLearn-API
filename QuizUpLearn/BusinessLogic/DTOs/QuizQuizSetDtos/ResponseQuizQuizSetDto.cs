using BusinessLogic.DTOs.QuizDtos;
using BusinessLogic.DTOs.QuizSetDtos;

namespace BusinessLogic.DTOs.QuizQuizSetDtos
{
    public class ResponseQuizQuizSetDto
    {
        public Guid Id { get; set; }
        public Guid QuizId { get; set; }
        public Guid QuizSetId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? DeletedAt { get; set; }
        
        // Navigation properties
        public QuizResponseDto? Quiz { get; set; }
        public QuizSetResponseDto? QuizSet { get; set; }
    }
}