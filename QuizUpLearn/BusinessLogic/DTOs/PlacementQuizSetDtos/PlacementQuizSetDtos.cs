namespace BusinessLogic.DTOs.PlacementQuizSetDtos
{
    public class PlacementQuizSetImportDto
    {
        public int Part { get; set; }
        public int GlobalIndex { get; set; }
        public string Prompt { get; set; } = string.Empty;
        public List<Choice> Choices { get; set; } = new();
        public string CorrectAnswer { get; set; } = string.Empty;
    }
    public class Choice
    {
        public string Label { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
    }
}
