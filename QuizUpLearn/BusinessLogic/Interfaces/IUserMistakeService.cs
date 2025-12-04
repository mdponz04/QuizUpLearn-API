using BusinessLogic.DTOs;
using BusinessLogic.DTOs.QuizDtos;
using BusinessLogic.DTOs.UserMistakeDtos;

namespace BusinessLogic.Interfaces
{
    public interface IUserMistakeService
    {
        Task<PaginationResponseDto<ResponseUserMistakeDto>> GetAllAsync(PaginationRequestDto pagination);
        Task<PaginationResponseDto<ResponseUserMistakeDto>> GetAllByUserIdAsync(Guid userId, PaginationRequestDto pagination);
        Task<ResponseUserMistakeDto?> GetByIdAsync(Guid id);
        Task<PaginationResponseDto<QuizResponseDto>> GetMistakeQuizzesByUserId(Guid userId, PaginationRequestDto pagination);
        Task AddAsync(RequestUserMistakeDto requestDto);
        Task UpdateAsync(Guid id, RequestUserMistakeDto requestDto);
        Task DeleteAsync(Guid id);
        Task CleanupOrphanWeakPointsAsync(Guid userId);
    }
}
