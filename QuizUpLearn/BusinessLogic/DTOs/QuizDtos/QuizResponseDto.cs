using BusinessLogic.DTOs.GrammarDtos;
using BusinessLogic.DTOs.QuizGroupItemDtos;
using BusinessLogic.DTOs.VocabularyDtos;

namespace BusinessLogic.DTOs.QuizDtos
{
    public class QuizResponseDto
    {
        public Guid Id { get; set; }
        public Guid? QuizGroupItemId { get; set; }
        public Guid? VocabularyId { get; set; }
        public Guid? GrammarId { get; set; }
        public string? QuestionText { get; set; }
        public string? CorrectAnswer { get; set; }
        public string? AudioURL { get; set; }
        public string? ImageURL { get; set; }
        public string? TOEICPart { get; set; }
        public int TimesAnswered { get; set; }
        public int TimesCorrect { get; set; }
        public int? OrderIndex { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? DeletedAt { get; set; }
        public List<ResponseAnswerOptionDto> AnswerOptions { get; set; } = new List<ResponseAnswerOptionDto>();
        public ResponseGrammarDto? Grammar { get; set; }
        public ResponseVocabularyDto? Vocabulary { get; set; }
        public ResponseQuizGroupItemDto? QuizGroupItem { get; set; }
    }
}
