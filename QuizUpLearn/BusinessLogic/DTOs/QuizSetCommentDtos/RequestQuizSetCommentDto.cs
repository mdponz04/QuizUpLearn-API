namespace BusinessLogic.DTOs.QuizSetCommentDtos
{
    public class RequestQuizSetCommentDto
    {
        public Guid UserId { get; set; }
        public Guid QuizSetId { get; set; }
        public string Content { get; set; } = string.Empty;
    }
}
