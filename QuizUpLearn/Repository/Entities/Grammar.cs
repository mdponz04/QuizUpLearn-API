using Repository.Entities.BaseModelEntity;
using Repository.Enums;

namespace Repository.Entities
{
    public class Grammar : BaseEntity
    {
        public required string Name { get; set; }
        public string? Tense { get; set; }
        public GrammarDifficultyEnum GrammarDifficulty { get; set; }
        public ICollection<Quiz> Quizzes { get; set; } = new List<Quiz>();
    }
}
