using BusinessLogic.DTOs;
using BusinessLogic.DTOs.VocabularyDtos;

namespace BusinessLogic.Interfaces
{
    public interface IVocabularyService
    {
        Task<PaginationResponseDto<ResponseVocabularyDto>> GetAllAsync(PaginationRequestDto pagination);
        Task<ResponseVocabularyDto?> GetByIdAsync(Guid id);
        Task<ResponseVocabularyDto?> CreateAsync(RequestVocabularyDto request);
        Task<ResponseVocabularyDto?> UpdateAsync(Guid id, RequestVocabularyDto request);
        Task<bool> DeleteAsync(Guid id);
    }
}

