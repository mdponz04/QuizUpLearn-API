using BusinessLogic.DTOs.SubscriptionDtos;
using BusinessLogic.DTOs;

namespace BusinessLogic.Interfaces
{
    public interface ISubscriptionService
    {
        Task<PaginationResponseDto<ResponseSubscriptionDto>> GetAllAsync(PaginationRequestDto pagination);
        Task<ResponseSubscriptionDto?> GetByIdAsync(Guid id);
        Task<ResponseSubscriptionDto> CreateAsync(RequestSubscriptionDto dto);
        Task<ResponseSubscriptionDto?> UpdateAsync(Guid id, RequestSubscriptionDto dto);
        Task<bool> DeleteAsync(Guid id);
    }
}
