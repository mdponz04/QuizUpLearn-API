namespace BusinessLogic.DTOs.GrammarDtos
{
    public class RequestGrammarDto
    {
        public required string Name { get; set; }
        public string? Tense { get; set; }
        public Repository.Enums.GrammarDifficultyEnum GrammarDifficulty { get; set; }
    }
}
