using BusinessLogic.DTOs.UserMistakeDtos;
using BusinessLogic.DTOs;

namespace BusinessLogic.Interfaces
{
    public interface IUserMistakeService
    {
        Task<PaginationResponseDto<ResponseUserMistakeDto>> GetAllAsync(PaginationRequestDto pagination);
        Task<PaginationResponseDto<ResponseUserMistakeDto>> GetAllByUserIdAsync(Guid userId, PaginationRequestDto pagination);
        Task<ResponseUserMistakeDto?> GetByIdAsync(Guid id);
        Task AddAsync(RequestUserMistakeDto requestDto);
        Task UpdateAsync(Guid id, RequestUserMistakeDto requestDto);
        Task DeleteAsync(Guid id);
    }
}
