using Repository.Entities.BaseModelEntity;
using Repository.Enums;

namespace Repository.Entities
{
    public class Vocabulary : BaseEntity
    {
        public required string KeyWord { get; set; }
        public VocabularyDifficultyEnum VocabularyDifficulty { get; set; }
        public string? ToeicPart { get; set; }
        public string? PassageType { get; set; }
        public ICollection<Quiz> Quizzes { get; set; } = new List<Quiz>();
    }
}