using BusinessLogic.DTOs;

namespace BusinessLogic.Interfaces
{
    public interface IAccountService
    {
        Task<ResponseAccountDto> CreateAsync(RequestAccountDto dto);
        Task<ResponseAccountDto?> GetByIdAsync(Guid id);
        Task<IEnumerable<ResponseAccountDto>> GetAllAsync(bool includeDeleted = false);
        Task<ResponseAccountDto?> UpdateAsync(Guid id, RequestAccountDto dto);
        Task<bool> SoftDeleteAsync(Guid id);
        Task<bool> RestoreAsync(Guid id);
    }
}


