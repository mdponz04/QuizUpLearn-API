namespace BusinessLogic.DTOs
{
    public class ResponseSubmitAnswersDto
    {
        public Guid AttemptId { get; set; }
        public int TotalQuestions { get; set; }
        public int CorrectAnswers { get; set; }
        public int WrongAnswers { get; set; }
        public int Score { get; set; }
        public decimal Accuracy { get; set; }
        public string Status { get; set; }
        public List<AnswerResultDto> AnswerResults { get; set; } = new();
        // DANGLING: Weak points phân tích bởi AI (có thể null nếu chạy nền)
        public IEnumerable<UserWeakPointDtos.ResponseUserWeakPointDto>? WeakPoints { get; set; }
    }

    public class AnswerResultDto
    {
        public Guid QuestionId { get; set; }
        public bool IsCorrect { get; set; }
        public Guid? CorrectAnswerOptionId { get; set; }
        public string? Explanation { get; set; }
    }
}

