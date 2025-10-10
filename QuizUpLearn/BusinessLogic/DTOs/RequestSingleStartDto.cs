namespace BusinessLogic.DTOs
{
    public class RequestSingleStartDto
    {
        public required Guid UserId { get; set; }
        public required Guid QuizSetId { get; set; }
        public int NumQuestions { get; set; } = 10;
    }
}


