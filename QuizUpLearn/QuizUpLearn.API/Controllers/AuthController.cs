using BusinessLogic.DTOs;
using BusinessLogic.Interfaces;
using BusinessLogic.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace QuizUpLearn.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IIdentityService _identityService;
        private readonly IAccountService _accountService;

        public AuthController(IIdentityService identityService, IAccountService accountService)
        {
            _identityService = identityService;
            _accountService = accountService;
        }

        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<IActionResult> Register([FromBody] RegisterRequestDto dto)
        {
            var result = await _identityService.RegisterAsync(dto);
            return Ok(result);
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto dto)
        {
            var login = await _identityService.LoginAsync(dto);
            if (login == null) return Unauthorized();

            var access = _identityService.GenerateJwtToken(login.Account);
            login.AccessToken = access.token;
            login.ExpiresAt = TimeZoneHelper.ConvertToVietnamTime(access.expiresAt);

            var refreshToken = _identityService.GenerateRefreshToken();
            login.RefreshToken = refreshToken;
            login.RefreshExpiresAt = TimeZoneHelper.ConvertToVietnamTime(DateTime.UtcNow.AddDays(30));
            
            await _identityService.SaveRefreshTokenAsync(login.Account.Id, refreshToken);
            
            return Ok(login);
        }

        [HttpPost("login-with-google")]
        [AllowAnonymous]
        public async Task<IActionResult> LoginWithGoogle([FromBody] GoogleLoginRequestDto dto)
        {
            var login = await _identityService.LoginWithGoogleAsync(dto);
            if (login == null) return Unauthorized(new { message = "Token không hợp lệ hoặc không thể xác thực" });

            var access = _identityService.GenerateJwtToken(login.Account);
            login.AccessToken = access.token;
            login.ExpiresAt = TimeZoneHelper.ConvertToVietnamTime(access.expiresAt);

            var refreshToken = _identityService.GenerateRefreshToken();
            login.RefreshToken = refreshToken;
            login.RefreshExpiresAt = TimeZoneHelper.ConvertToVietnamTime(DateTime.UtcNow.AddDays(30));
            
            await _identityService.SaveRefreshTokenAsync(login.Account.Id, refreshToken);
            
            return Ok(login);
        }

        [HttpPost("reset-password/initiate")]
        [AllowAnonymous]
        public async Task<IActionResult> InitiateResetPassword([FromBody] ResetPasswordInitiateRequestDto dto)
        {
            var ok = await _identityService.InitiateResetPasswordAsync(dto);
            return Ok(new { success = ok });
        }

        [HttpPost("reset-password/verify")]
        [AllowAnonymous]
        public async Task<IActionResult> VerifyResetPassword([FromBody] ResetPasswordVerifyRequestDto dto)
        {
            var ok = await _identityService.VerifyResetPasswordOtpAsync(dto);
            return Ok(new { isValid = ok });
        }

        [HttpPost("reset-password/confirm")]
        [AllowAnonymous]
        public async Task<IActionResult> ConfirmResetPassword([FromBody] ResetPasswordConfirmRequestDto dto)
        {
            var ok = await _identityService.ConfirmResetPasswordAsync(dto);
            if (!ok) return BadRequest("OTP không hợp lệ hoặc đã hết hạn");
            return Ok(new { success = true });
        }

        [HttpPost("refresh")]
        [AllowAnonymous]
        public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequestDto dto)
        {
            var isValid = await _identityService.ValidateRefreshTokenAsync(dto.AccountId, dto.RefreshToken);
            if (!isValid) return Unauthorized();

            await _identityService.DeleteRefreshTokenAsync(dto.AccountId, dto.RefreshToken);

            var account = await _accountService.GetByIdAsync(dto.AccountId);
            if (account == null) return Unauthorized();

            var access = _identityService.GenerateJwtToken(account);
            
            var newRefreshToken = _identityService.GenerateRefreshToken();
            await _identityService.SaveRefreshTokenAsync(dto.AccountId, newRefreshToken);

            return Ok(new
            {
                AccessToken = access.token,
                ExpiresAt = TimeZoneHelper.ConvertToVietnamTime(access.expiresAt),
                RefreshToken = newRefreshToken,
                RefreshExpiresAt = TimeZoneHelper.ConvertToVietnamTime(DateTime.UtcNow.AddDays(30))
            });
        }

        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout([FromBody] RefreshTokenRequestDto dto)
        {
            await _identityService.DeleteRefreshTokenAsync(dto.AccountId, dto.RefreshToken);
            return Ok(new { success = true });
        }

    }
}


