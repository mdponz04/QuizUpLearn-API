using BusinessLogic.DTOs;
using BusinessLogic.DTOs.UserReportDtos;

namespace BusinessLogic.Interfaces
{
    public interface IUserReportService
    {
        Task<ResponseUserReportDto> CreateAsync(RequestUserReportDto dto);
        Task<ResponseUserReportDto?> GetByIdAsync(Guid id);
        Task<PaginationResponseDto<ResponseUserReportDto>> GetAllAsync(PaginationRequestDto pagination, bool includeDeleted = false);
        Task<PaginationResponseDto<ResponseUserReportDto>> GetByUserIdAsync(Guid userId, PaginationRequestDto pagination, bool includeDeleted = false);
        Task<bool> HardDeleteAsync(Guid id);
        Task<bool> IsExistAsync(Guid userId);
    }
}


