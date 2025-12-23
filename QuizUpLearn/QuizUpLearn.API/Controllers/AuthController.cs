using BusinessLogic.DTOs;
using BusinessLogic.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace QuizUpLearn.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IIdentityService _identityService;
        private readonly IConfiguration _configuration;

        public AuthController(IIdentityService identityService, IConfiguration configuration)
        {
            _identityService = identityService;
            _configuration = configuration;
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

            var access = GenerateJwtToken(login.Account);
            var refresh = GenerateRefreshToken(login.Account);
            login.AccessToken = access.token;
            login.ExpiresAt = access.expiresAt;
            login.RefreshToken = refresh.token;
            login.RefreshExpiresAt = refresh.expiresAt;
            return Ok(login);
        }

        [HttpPost("login-with-google")]
        [AllowAnonymous]
        public async Task<IActionResult> LoginWithGoogle([FromBody] GoogleLoginRequestDto dto)
        {
            var login = await _identityService.LoginWithGoogleAsync(dto);
            if (login == null) return Unauthorized(new { message = "Token không hợp lệ hoặc không thể xác thực" });

            var access = GenerateJwtToken(login.Account);
            var refresh = GenerateRefreshToken(login.Account);
            login.AccessToken = access.token;
            login.ExpiresAt = access.expiresAt;
            login.RefreshToken = refresh.token;
            login.RefreshExpiresAt = refresh.expiresAt;
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

        private (string token, DateTime expiresAt) GenerateJwtToken(ResponseAccountDto account)
        {
            var jwt = _configuration.GetSection("Jwt");
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["Key"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expires = DateTime.UtcNow.AddMinutes(int.Parse(jwt["ExpiresMinutes"]!));

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, account.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, account.Email ?? string.Empty),
                new Claim("userId", account.UserId.ToString()),
                new Claim("roleId", account.RoleId.ToString()),
                new Claim("roleName", account.RoleName ?? string.Empty)
            };

            var token = new JwtSecurityToken(
                issuer: jwt["Issuer"],
                audience: jwt["Audience"],
                claims: claims,
                expires: expires,
                signingCredentials: creds
            );

            return (new JwtSecurityTokenHandler().WriteToken(token), expires);
        }

        [HttpPost("refresh")]
        [AllowAnonymous]
        public IActionResult Refresh([FromBody] RefreshTokenRequestDto dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.RefreshToken))
                return BadRequest(new { message = "Refresh token is required" });

            var (isValid, principal, errorMessage) = ValidateRefreshToken(dto.RefreshToken);
            if (!isValid || principal == null)
                return Unauthorized(new { message = errorMessage ?? "Invalid refresh token" });

            var accountId = principal.FindFirstValue(JwtRegisteredClaimNames.Sub);
            var email = principal.FindFirstValue(JwtRegisteredClaimNames.Email) ?? string.Empty;
            var userIdStr = principal.FindFirst("userId")?.Value ?? Guid.Empty.ToString();
            var roleIdStr = principal.FindFirst("roleId")?.Value ?? Guid.Empty.ToString();
            var roleName = principal.FindFirst("roleName")?.Value ?? string.Empty;

            if (string.IsNullOrEmpty(accountId) || !Guid.TryParse(accountId, out var accountIdGuid))
                return Unauthorized(new { message = "Invalid token claims" });

            if (string.IsNullOrEmpty(roleIdStr) || !Guid.TryParse(roleIdStr, out var roleIdGuid))
                return Unauthorized(new { message = "Invalid role ID in token" });

            var account = new ResponseAccountDto
            {
                Id = accountIdGuid,
                Email = email,
                Username = string.Empty,
                AvatarUrl = string.Empty,
                UserId = Guid.TryParse(userIdStr, out var userId) ? userId : Guid.Empty,
                RoleId = roleIdGuid,
                RoleName = roleName,
                IsEmailVerified = false,
                LastLoginAt = null,
                LoginAttempts = 0,
                LockoutUntil = null,
                IsActive = true,
                IsBanned = false,
                CreatedAt = DateTime.MinValue,
                UpdatedAt = null,
                DeletedAt = null
            };

            var access = GenerateJwtToken(account);
            var refresh = GenerateRefreshToken(account);

            return Ok(new LoginResponseDto
            {
                Account = account,
                AccessToken = access.token,
                ExpiresAt = access.expiresAt,
                RefreshToken = refresh.token,
                RefreshExpiresAt = refresh.expiresAt
            });
        }

        private (string token, DateTime expiresAt) GenerateRefreshToken(ResponseAccountDto account)
        {
            var jwt = _configuration.GetSection("Jwt");
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["Key"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expires = DateTime.UtcNow.AddDays(int.Parse(jwt["RefreshExpiresDays"]!));

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, account.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, account.Email ?? string.Empty),
                new Claim("userId", account.UserId.ToString()),
                new Claim("roleId", account.RoleId.ToString()),
                new Claim("roleName", account.RoleName ?? string.Empty),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var token = new JwtSecurityToken(
                issuer: jwt["Issuer"],
                audience: jwt["Audience"],
                claims: claims,
                expires: expires,
                signingCredentials: creds
            );
            return (new JwtSecurityTokenHandler().WriteToken(token), expires);
        }

        private (bool isValid, ClaimsPrincipal? principal, string? errorMessage) ValidateRefreshToken(string refreshToken)
        {
            var jwt = _configuration.GetSection("Jwt");
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(jwt["Key"]!);
            
            if (!tokenHandler.CanReadToken(refreshToken))
            {
                return (false, null, "Invalid token format");
            }

            try
            {
                var principal = tokenHandler.ValidateToken(refreshToken, new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwt["Issuer"],
                    ValidAudience = jwt["Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ClockSkew = TimeSpan.Zero // Không cho phép clock skew để tránh lỗi timing
                }, out var validatedToken);

                // Kiểm tra xem token có phải là refresh token không (có JTI claim)
                var jti = principal.FindFirst(JwtRegisteredClaimNames.Jti)?.Value;
                if (string.IsNullOrEmpty(jti))
                {
                    return (false, null, "Token is not a valid refresh token");
                }

                return (true, principal, null);
            }
            catch (SecurityTokenExpiredException)
            {
                return (false, null, "Refresh token has expired");
            }
            catch (SecurityTokenInvalidSignatureException)
            {
                return (false, null, "Invalid token signature");
            }
            catch (SecurityTokenInvalidIssuerException)
            {
                return (false, null, "Invalid token issuer");
            }
            catch (SecurityTokenInvalidAudienceException)
            {
                return (false, null, "Invalid token audience");
            }
            catch (Exception ex)
            {
                return (false, null, $"Token validation failed: {ex.Message}");
            }
        }
    }
}


