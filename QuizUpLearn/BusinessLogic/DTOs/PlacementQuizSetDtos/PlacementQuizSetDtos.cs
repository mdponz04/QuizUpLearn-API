namespace BusinessLogic.DTOs.PlacementQuizSetDtos
{
    public class PlacementQuizSetImportDto
    {
        public int Part { get; set; }
        public int GlobalIndex { get; set; }
        public int IndexInPart { get; set; }
        public string? GroupId { get; set; }
        public string? Audio { get; set; }
        public string? Passage { get; set; }
        public string Prompt { get; set; } = "";
        public List<Choice> Choices { get; set; } = new();
        public string CorrectAnswer { get; set; } = "";
        public string? Explanation { get; set; }
        public string? Tags { get; set; }
    }
    public class Choice
    {
        public string Label { get; set; } = "";
        public string Text { get; set; } = "";
    }
}
