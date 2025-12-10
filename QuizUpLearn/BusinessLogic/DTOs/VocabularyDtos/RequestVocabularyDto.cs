namespace BusinessLogic.DTOs.VocabularyDtos
{
    public class RequestVocabularyDto
    {
        public required string KeyWord { get; set; }
        public Repository.Enums.VocabularyDifficultyEnum VocabularyDifficulty { get; set; }
        public string? ToeicPart { get; set; }
        public string? PassageType { get; set; }
    }
}
