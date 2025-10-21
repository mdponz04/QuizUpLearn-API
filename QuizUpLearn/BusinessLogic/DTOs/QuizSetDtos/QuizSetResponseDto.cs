using BusinessLogic.DTOs.QuizDtos;

namespace BusinessLogic.DTOs.QuizSetDtos
{
    public class QuizSetResponseDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string QuizType { get; set; }
        public string TOEICPart { get; set; }
        public string SkillType { get; set; }
        public string DifficultyLevel { get; set; }
        public int TotalQuestions { get; set; }
        public int? TimeLimit { get; set; }
        public Guid CreatedBy { get; set; }
        public bool IsAIGenerated { get; set; }
        public bool IsPublished { get; set; }
        public bool IsPremiumOnly { get; set; }
        public int TotalAttempts { get; set; }
        public decimal AverageScore { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? DeletedAt { get; set; }
        public List<QuizResponseDto> Quizzes { get; set; } = new List<QuizResponseDto>();
    }
}
