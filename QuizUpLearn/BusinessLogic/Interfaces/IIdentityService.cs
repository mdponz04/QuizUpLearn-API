using BusinessLogic.DTOs;
using System.Security.Claims;

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
        
        // Token generation methods
        (string token, DateTime expiresAt) GenerateJwtToken(ResponseAccountDto account);
        
        // Refresh token methods
        string GenerateRefreshToken();
        Task SaveRefreshTokenAsync(Guid accountId, string refreshToken);
        Task<bool> ValidateRefreshTokenAsync(Guid accountId, string refreshToken);
        Task DeleteRefreshTokenAsync(Guid accountId, string refreshToken);
    }
}


