using BusinessLogic.DTOs;
using BusinessLogic.Interfaces;
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
        public async Task<IActionResult> Register([FromBody] RegisterRequestDto dto)
        {
            var result = await _identityService.RegisterAsync(dto);
            return Ok(result);
        }

        [HttpPost("login")]
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

        [HttpPost("reset-password/initiate")]
        public async Task<IActionResult> InitiateResetPassword([FromBody] ResetPasswordInitiateRequestDto dto)
        {
            var ok = await _identityService.InitiateResetPasswordAsync(dto);
            return Ok(new { success = ok });
        }

        [HttpPost("reset-password/verify")]
        public async Task<IActionResult> VerifyResetPassword([FromBody] ResetPasswordVerifyRequestDto dto)
        {
            var ok = await _identityService.VerifyResetPasswordOtpAsync(dto);
            return Ok(new { isValid = ok });
        }

        [HttpPost("reset-password/confirm")]
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
                new Claim("roleId", account.RoleId.ToString())
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
        public IActionResult Refresh([FromBody] RefreshTokenRequestDto dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.RefreshToken))
                return BadRequest();

            var (isValid, principal) = ValidateRefreshToken(dto.RefreshToken);
            if (!isValid || principal == null)
                return Unauthorized();

            var accountId = principal.FindFirstValue(JwtRegisteredClaimNames.Sub);
            var email = principal.FindFirstValue(JwtRegisteredClaimNames.Email) ?? string.Empty;
            var roleIdStr = principal.FindFirst("roleId")?.Value ?? Guid.Empty.ToString();

            var account = new ResponseAccountDto
            {
                Id = Guid.Parse(accountId),
                Email = email,
                RoleId = Guid.Parse(roleIdStr),
                UserId = Guid.Empty,
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
                new Claim("roleId", account.RoleId.ToString()),
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

        private (bool isValid, ClaimsPrincipal? principal) ValidateRefreshToken(string refreshToken)
        {
            var jwt = _configuration.GetSection("Jwt");
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(jwt["Key"]!);
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
                    IssuerSigningKey = new SymmetricSecurityKey(key)
                }, out var _);
                return (true, principal);
            }
            catch
            {
                return (false, null);
            }
        }
    }
}


