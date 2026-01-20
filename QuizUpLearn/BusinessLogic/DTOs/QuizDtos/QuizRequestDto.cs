namespace BusinessLogic.DTOs.QuizDtos
{
    public class QuizRequestDto
    {
        public Guid? QuizGroupItemId { get; set; }
        public Guid? VocabularyId { get; set; }
        public Guid? GrammarId { get; set; }
        public string? QuestionText { get; set; }
        public string? TOEICPart { get; set; }
        public bool IsActive { get; set; } = true;
        public bool IsAIGenerated { get; set; } = true;
        public string DifficultyLevel { get; set; } = string.Empty;
        public List<RequestAnswerOptionDto> AnswerOptions { get; set; } = new List<RequestAnswerOptionDto>();
        public string CorrectAnswer { get; set; } = string.Empty;
        public string? AudioURL { get; set; }
        public string? AudioScript { get; set; }
        public string? ImageURL { get; set; }
        public string? ImageDescription { get; set; }
        public int? OrderIndex { get; set; }
    }
    
}
