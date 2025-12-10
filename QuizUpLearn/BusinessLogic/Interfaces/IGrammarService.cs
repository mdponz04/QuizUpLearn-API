using BusinessLogic.DTOs;
using BusinessLogic.DTOs.GrammarDtos;

namespace BusinessLogic.Interfaces
{
    public interface IGrammarService
    {
        Task<PaginationResponseDto<ResponseGrammarDto>> GetAllAsync(PaginationRequestDto pagination);
        Task<ResponseGrammarDto?> GetByIdAsync(Guid id);
        Task<ResponseGrammarDto?> CreateAsync(RequestGrammarDto request);
        Task<ResponseGrammarDto?> UpdateAsync(Guid id, RequestGrammarDto request);
        Task<bool> DeleteAsync(Guid id);
    }
}

