using BusinessLogic.DTOs;

namespace BusinessLogic.Interfaces
{
    public interface IIdentityService
    {
        Task<ResponseAccountDto> RegisterAsync(RegisterRequestDto dto);
        Task<LoginResponseDto?> LoginAsync(LoginRequestDto dto);
        Task<LoginResponseDto?> LoginWithGoogleAsync(GoogleLoginRequestDto dto);
        Task<bool> InitiateResetPasswordAsync(ResetPasswordInitiateRequestDto dto);
        Task<bool> VerifyResetPasswordOtpAsync(ResetPasswordVerifyRequestDto dto);
        Task<bool> ConfirmResetPasswordAsync(ResetPasswordConfirmRequestDto dto);
    }
}


