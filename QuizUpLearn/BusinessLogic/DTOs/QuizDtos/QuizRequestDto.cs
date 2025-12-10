namespace BusinessLogic.DTOs.QuizDtos
{
    public class QuizRequestDto
    {
        public Guid? QuizGroupItemId { get; set; }
        public string? QuestionText { get; set; }
        public string? TOEICPart { get; set; }
        public bool IsActive { get; set; } = true;
        public List<RequestAnswerOptionDto> AnswerOptions { get; set; } = new List<RequestAnswerOptionDto>();
        // optional fields
        public string CorrectAnswer { get; set; } = string.Empty;
        public string? AudioURL { get; set; }
        public string? ImageURL { get; set; }
        public int? OrderIndex { get; set; }
    }
    
}
