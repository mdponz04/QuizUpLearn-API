using BusinessLogic.DTOs;

namespace BusinessLogic.Interfaces
{
    public interface IIdentityService
    {
        Task<ResponseAccountDto> RegisterAsync(RegisterRequestDto dto);
        Task<LoginResponseDto?> LoginAsync(LoginRequestDto dto);
    }
}


