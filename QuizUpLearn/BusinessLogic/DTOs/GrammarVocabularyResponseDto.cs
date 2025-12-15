using BusinessLogic.DTOs.GrammarDtos;
using BusinessLogic.DTOs.VocabularyDtos;

namespace BusinessLogic.DTOs
{
    public class GrammarVocabularyResponseDto
    {
        public ResponseGrammarDto? Grammar { get; set; }
        public ResponseVocabularyDto? Vocabulary { get; set; }
        public string? Part { get; set; }
    }
}
