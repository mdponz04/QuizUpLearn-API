using BusinessLogic.DTOs.QuizGroupItemDtos;

namespace BusinessLogic.Interfaces
{
    public interface IQuizGroupItemService
    {
        Task<IEnumerable<ResponseQuizGroupItemDto>> GetAllAsync();
        Task<ResponseQuizGroupItemDto?> GetByIdAsync(Guid id);
        Task<ResponseQuizGroupItemDto?> AddAsync(RequestQuizGroupItemDto requestDto);
        Task<ResponseQuizGroupItemDto?> UpdateAsync(Guid id, RequestQuizGroupItemDto requestDto);
        Task<bool> DeleteAsync(Guid id);
    }
}
