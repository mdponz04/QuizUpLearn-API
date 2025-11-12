using BusinessLogic.DTOs.QuizGroupItemDtos;
using BusinessLogic.DTOs;

namespace BusinessLogic.Interfaces
{
    public interface IQuizGroupItemService
    {
        Task<PaginationResponseDto<ResponseQuizGroupItemDto>> GetAllAsync(PaginationRequestDto pagination);
        Task<PaginationResponseDto<ResponseQuizGroupItemDto>> GetAllByQuizSetIdAsync(Guid quizGroupId, PaginationRequestDto pagination);
        Task<ResponseQuizGroupItemDto?> GetByIdAsync(Guid id);
        Task<ResponseQuizGroupItemDto?> CreateAsync(RequestQuizGroupItemDto requestDto);
        Task<ResponseQuizGroupItemDto?> UpdateAsync(Guid id, RequestQuizGroupItemDto requestDto);
        Task<bool> DeleteAsync(Guid id);
    }
}
