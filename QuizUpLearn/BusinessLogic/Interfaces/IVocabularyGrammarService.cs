using BusinessLogic.DTOs;

namespace BusinessLogic.Interfaces
{
    public interface IVocabularyGrammarService
    {
        Task<PaginationResponseDto<GrammarVocabularyResponseDto>> GetUnusedPairVocabularyGrammar(PaginationRequestDto pagination = null!);
    }
}
