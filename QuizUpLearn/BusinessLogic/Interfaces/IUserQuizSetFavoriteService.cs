using BusinessLogic.DTOs;
using BusinessLogic.DTOs.UserQuizSetFavoriteDtos;

namespace BusinessLogic.Interfaces
{
    public interface IUserQuizSetFavoriteService
    {
        Task<ResponseUserQuizSetFavoriteDto> CreateAsync(RequestUserQuizSetFavoriteDto dto);
        Task<ResponseUserQuizSetFavoriteDto?> GetByIdAsync(Guid id);
        Task<PaginationResponseDto<ResponseUserQuizSetFavoriteDto>> GetAllAsync(PaginationRequestDto pagination, bool includeDeleted = false);
        Task<PaginationResponseDto<ResponseUserQuizSetFavoriteDto>> GetByUserIdAsync(Guid userId, PaginationRequestDto pagination, bool includeDeleted = false);
        Task<PaginationResponseDto<ResponseUserQuizSetFavoriteDto>> GetByQuizSetIdAsync(Guid quizSetId, PaginationRequestDto pagination, bool includeDeleted = false);
        Task<ResponseUserQuizSetFavoriteDto?> GetByUserAndQuizSetAsync(Guid userId, Guid quizSetId, bool includeDeleted = false);
        Task<bool> ToggleFavoriteAsync(Guid userId, Guid quizSetId);
        Task<bool> HardDeleteAsync(Guid id);
        Task<bool> IsExistAsync(Guid userId, Guid quizSetId);
        Task<int> GetFavoriteCountByQuizSetAsync(Guid quizSetId);
    }
}
