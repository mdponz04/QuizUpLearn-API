using BusinessLogic.DTOs.QuizSetDtos;

namespace BusinessLogic.DTOs.UserQuizSetLikeDtos
{
    public class ResponseUserQuizSetLikeDto
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public Guid QuizSetId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? DeletedAt { get; set; }
        
        // Navigation properties
        public ResponseUserDto? User { get; set; }
        public QuizSetResponseDto? QuizSet { get; set; }
    }
}
