using Repository.Enums;

namespace BusinessLogic.DTOs.GrammarDtos
{
    public class RequestGrammarDto
    {
        public required string Name { get; set; }
        public string? Tense { get; set; }
        public GrammarDifficultyEnum GrammarDifficulty { get; set; }
    }
}
