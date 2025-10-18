namespace BusinessLogic.DTOs.QuizDtos
{
    public class QuizRequestDto
    {
        public Guid QuizSetId { get; set; }
        public required string QuestionText { get; set; }
        public required string TOEICPart { get; set; }
        public bool IsActive { get; set; } = true;
        public List<RequestAnswerOptionDto> AnswerOptions { get; set; } = new List<RequestAnswerOptionDto>();
        // optional fields
        public string GroupId { get; set; } = string.Empty;
        public string CorrectAnswer { get; set; } = string.Empty;
        public string AudioURL { get; set; } = string.Empty;
        public string ImageURL { get; set; } = string.Empty;
        public int? OrderIndex { get; set; }
    }
    
}
