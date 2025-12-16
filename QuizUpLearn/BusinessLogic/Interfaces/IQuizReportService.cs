using BusinessLogic.DTOs;
using BusinessLogic.DTOs.QuizReportDtos;

namespace BusinessLogic.Interfaces
{
    public interface IQuizReportService
    {
        Task<ResponseQuizReportDto> CreateAsync(RequestQuizReportDto dto);
        Task<ResponseQuizReportDto?> GetByIdAsync(Guid id);
        Task<PaginationResponseDto<ResponseQuizReportDto>> GetAllAsync(PaginationRequestDto pagination, bool includeDeleted = false);
        Task<PaginationResponseDto<ResponseQuizReportDto>> GetByUserIdAsync(Guid userId, PaginationRequestDto pagination, bool includeDeleted = false);
        Task<PaginationResponseDto<ResponseQuizReportDto>> GetByQuizIdAsync(Guid quizId, PaginationRequestDto pagination, bool includeDeleted = false);
        Task<ResponseQuizReportDto?> GetByUserAndQuizAsync(Guid userId, Guid quizId, bool includeDeleted = false);
        Task<bool> SoftDeleteAsync(Guid id);
        Task<bool> HardDeleteAsync(Guid id);
        Task<bool> IsExistAsync(Guid userId, Guid quizId);
    }
}
