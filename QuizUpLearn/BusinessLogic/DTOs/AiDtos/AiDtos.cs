namespace BusinessLogic.DTOs.AiDtos
{
    public class AiGenerateQuizSetRequestDto
    {
        public int QuestionQuantity { get; set; }
        public required string Difficulty { get; set; }
        public required string Topic { get; set; }
        public Guid CreatorId { get; set; }
    }
    public class AiGenerateQuizResponseDto
    {
        public string? AudioScript { get; set; }
        public string? ImageDescription { get; set; }
        public string? QuestionText { get; set; }
        public List<AiGenerateAnswerOptionResponseDto> AnswerOptions { get; set; }
    }
    public class AiGenerateAnswerOptionResponseDto
    {
        public string OptionLabel { get; set; }
        public string OptionText { get; set; }
        public bool IsCorrect { get; set; }
    }
    public class AIValidationResponseDto
    {
        public bool IsValid { get; set; }
        public string Feedback { get; set; }
    }
}
