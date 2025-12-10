namespace BusinessLogic.DTOs.VocabularyDtos
{
    public class ResponseVocabularyDto
    {
        public Guid Id { get; set; }
        public string KeyWord { get; set; } = string.Empty;
        public Repository.Enums.VocabularyDifficultyEnum VocabularyDifficulty { get; set; }
        public string? ToeicPart { get; set; }
        public string? PassageType { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
