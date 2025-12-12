using BusinessLogic.DTOs;
using BusinessLogic.DTOs.GrammarDtos;
using BusinessLogic.DTOs.VocabularyDtos;

namespace BusinessLogic.Interfaces
{
    public interface IVocabularyGrammarService
    {
        Task<PaginationResponseDto<(ResponseGrammarDto, ResponseVocabularyDto, string)>> GetUnusedPairVocabularyGrammar();
    }
}
