using BusinessLogic.DTOs.QuizGroupItemDtos;
using BusinessLogic.DTOs;

namespace BusinessLogic.Interfaces
{
    public interface IQuizGroupItemService
    {
        Task<PaginationResponseDto<ResponseQuizGroupItemDto>> GetAllGroupItemAsync(PaginationRequestDto pagination);
        Task<ResponseQuizGroupItemDto?> GetGroupItemByIdAsync(Guid id);
        Task<ResponseQuizGroupItemDto?> CreateGroupItemAsync(RequestQuizGroupItemDto requestDto);
        Task<ResponseQuizGroupItemDto?> UpdateGroupItemAsync(Guid id, RequestQuizGroupItemDto requestDto);
        Task<bool> DeleteGroupItemAsync(Guid id);
    }
}
