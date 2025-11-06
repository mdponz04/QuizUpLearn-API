namespace BusinessLogic.DTOs.QuizGroupItemDtos
{
    public class RequestQuizGroupItemDto
    {
        public Guid? QuizSetId { get; set; }
        public string? Name { get; set; }
        public string? AudioUrl { get; set; }
        public string? ImageUrl { get; set; }
        public string? AudioScript { get; set; }
        public string? ImageDescription { get; set; }
        public string? PassageText { get; set; }
    }
}
