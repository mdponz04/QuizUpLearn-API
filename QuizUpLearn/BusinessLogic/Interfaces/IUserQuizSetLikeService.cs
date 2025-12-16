using BusinessLogic.DTOs;
using BusinessLogic.DTOs.UserQuizSetLikeDtos;

namespace BusinessLogic.Interfaces
{
    public interface IUserQuizSetLikeService
    {
        Task<ResponseUserQuizSetLikeDto> CreateAsync(RequestUserQuizSetLikeDto dto);
        Task<ResponseUserQuizSetLikeDto?> GetByIdAsync(Guid id);
        Task<PaginationResponseDto<ResponseUserQuizSetLikeDto>> GetAllAsync(PaginationRequestDto pagination, bool includeDeleted = false);
        Task<PaginationResponseDto<ResponseUserQuizSetLikeDto>> GetByUserIdAsync(Guid userId, PaginationRequestDto pagination, bool includeDeleted = false);
        Task<PaginationResponseDto<ResponseUserQuizSetLikeDto>> GetByQuizSetIdAsync(Guid quizSetId, PaginationRequestDto pagination, bool includeDeleted = false);
        Task<ResponseUserQuizSetLikeDto?> GetByUserAndQuizSetAsync(Guid userId, Guid quizSetId, bool includeDeleted = false);
        Task<bool> ToggleLikeAsync(Guid userId, Guid quizSetId);
        Task<bool> SoftDeleteAsync(Guid id);
        Task<bool> HardDeleteAsync(Guid id);
        Task<bool> IsExistAsync(Guid userId, Guid quizSetId);
        Task<int> GetLikeCountByQuizSetAsync(Guid quizSetId);
    }
}
