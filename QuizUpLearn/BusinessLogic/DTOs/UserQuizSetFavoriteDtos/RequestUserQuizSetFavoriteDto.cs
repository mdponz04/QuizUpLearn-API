namespace BusinessLogic.DTOs.UserQuizSetFavoriteDtos
{
    public class RequestUserQuizSetFavoriteDto
    {
        public Guid UserId { get; set; }
        public Guid QuizSetId { get; set; }
    }
}
