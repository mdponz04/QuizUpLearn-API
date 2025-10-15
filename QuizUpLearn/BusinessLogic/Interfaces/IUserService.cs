using BusinessLogic.DTOs;

namespace BusinessLogic.Interfaces
{
    public interface IUserService
    {
        Task<ResponseUserDto> CreateAsync(RequestUserDto dto);
        Task<IEnumerable<ResponseUserDto>> GetAllAsync(bool includeDeleted = false);
        Task<ResponseUserDto?> GetByIdAsync(Guid id);
        Task<ResponseUserDto?> GetByUsernameAsync(string username);
        Task<ResponseUserDto?> GetByAccountIdAsync(Guid accountId);
        Task<ResponseUserDto?> UpdateAsync(Guid id, RequestUserDto dto);
        Task<bool> SoftDeleteAsync(Guid id);
        Task<bool> RestoreAsync(Guid id);
    }
}
