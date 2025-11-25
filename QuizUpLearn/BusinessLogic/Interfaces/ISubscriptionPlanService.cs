using BusinessLogic.DTOs.SubscriptionPlanDtos;
using BusinessLogic.DTOs;

namespace BusinessLogic.Interfaces
{
    public interface ISubscriptionPlanService
    {
        Task<PaginationResponseDto<ResponseSubscriptionPlanDto>> GetAllAsync(PaginationRequestDto pagination);
        Task<ResponseSubscriptionPlanDto?> GetByIdAsync(Guid id);
        Task<ResponseSubscriptionPlanDto> CreateAsync(RequestSubscriptionPlanDto dto);
        Task<ResponseSubscriptionPlanDto?> UpdateAsync(Guid id, RequestSubscriptionPlanDto dto);
        Task<ResponseSubscriptionPlanDto> GetFreeSubscriptionPlanAsync();
        Task<bool> DeleteAsync(Guid id);
    }
}
