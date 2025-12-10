namespace BusinessLogic.DTOs.GrammarDtos
{
    public class ResponseGrammarDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Tense { get; set; }
        public Repository.Enums.GrammarDifficultyEnum GrammarDifficulty { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
