namespace BusinessLogic.DTOs
{
    public class PlayerHistoryRequestDto
    {
        public Guid UserId { get; set; }
        public Guid? QuizSetId { get; set; } // Optional filter by quiz set
        public string? Status { get; set; } // "completed", "in_progress", null for all
        public string? AttemptType { get; set; } // "single", "multiplayer", null for all
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string SortBy { get; set; } = "CreatedAt"; // "CreatedAt", "Score", "Accuracy"
        public string SortOrder { get; set; } = "desc"; // "asc", "desc"
    }
}
