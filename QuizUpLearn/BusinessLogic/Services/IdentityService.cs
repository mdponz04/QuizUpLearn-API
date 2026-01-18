using AutoMapper;
using BusinessLogic.DTOs;
using BusinessLogic.Interfaces;
using BusinessLogic.DTOs.SubscriptionDtos;
using Repository.Entities;
using Repository.Interfaces;
using BCrypt.Net;
using Microsoft.Extensions.Configuration;
using FirebaseAdmin.Auth;
using BusinessLogic.Helpers;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Caching.Distributed;

namespace BusinessLogic.Services
{
    public class IdentityService : IIdentityService
    {
        private readonly IAccountRepo _accountRepo;
        private readonly IUserRepo _userRepo;
        private readonly IMapper _mapper;
        private readonly IOtpVerificationRepo _otpRepo;
        private readonly IMailerSendService _mailer;
        private readonly IConfiguration _configuration;
        private readonly ISubscriptionService _subscriptionService;
        private readonly ISubscriptionPlanService _subscriptionPlanService;
        private readonly IDistributedCache _cache;

        public IdentityService(
            IAccountRepo accountRepo,
            IUserRepo userRepo,
            IOtpVerificationRepo otpRepo,
            IMailerSendService mailer,
            IConfiguration configuration,
            IMapper mapper,
            ISubscriptionService subscriptionService,
            ISubscriptionPlanService subscriptionPlanService,
            IDistributedCache cache)
        {
            _accountRepo = accountRepo;
            _userRepo = userRepo;
            _otpRepo = otpRepo;
            _mailer = mailer;
            _configuration = configuration;
            _mapper = mapper;
            _subscriptionService = subscriptionService;
            _subscriptionPlanService = subscriptionPlanService;
            _cache = cache;
        }

        public async Task<LoginResponseDto?> LoginAsync(LoginRequestDto dto)
        {
            var email = dto.Email.Trim().ToLowerInvariant();
            var account = await _accountRepo.GetByEmailAsync(email);
            if (account == null) return null;
            if (!BCrypt.Net.BCrypt.Verify(dto.Password, account.PasswordHash)) return null;
            if (account.DeletedAt != null || !account.IsActive) return null;

            account.LastLoginAt = DateTime.UtcNow;
            await _accountRepo.UpdateAsync(account.Id, account);
            // Token sẽ được tạo ở Controller, service chỉ trả về Account (đơn giản hóa theo yêu cầu)
            return new LoginResponseDto
            {
                Account = _mapper.Map<ResponseAccountDto>(account),
                AccessToken = string.Empty,
                ExpiresAt = DateTime.UtcNow,
                RefreshToken = string.Empty,
                RefreshExpiresAt = DateTime.UtcNow
            };
        }

        public async Task<ResponseAccountDto> RegisterAsync(RegisterRequestDto dto)
        {
            var email = dto.Email.Trim().ToLowerInvariant();
            var existing = await _accountRepo.GetByEmailAsync(email);
            if (existing != null) throw new ArgumentException("Email đã tồn tại");

            var entity = new Account
            {
                Email = email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password)
            };
            var created = await _accountRepo.CreateAsync(entity);

            var freePlan = await _subscriptionPlanService.GetFreeSubscriptionPlanAsync();
            await _subscriptionService.CreateAsync(new RequestSubscriptionDto
            {
                UserId = created.UserId,
                SubscriptionPlanId = freePlan.Id,
                EndDate = DateTime.UtcNow.AddDays(freePlan.DurationDays)
            });

            return _mapper.Map<ResponseAccountDto>(created);
        }

        public async Task<bool> InitiateResetPasswordAsync(ResetPasswordInitiateRequestDto dto)
        {
            var email = dto.Email.Trim().ToLowerInvariant();
            var account = await _accountRepo.GetByEmailAsync(email);
            if (account == null) return true; 

            const string purpose = "RESET_PASSWORD";
            await _otpRepo.InvalidateAllActiveOtpsAsync(email, purpose);

            var otp = GenerateSixDigitOtp();
            var entity = new OtpVerification
            {
                AccountId = account.Id,
                Email = email,
                OTPCode = otp,
                Purpose = purpose,
                IsUsed = false,
                ExpiresAt = DateTime.UtcNow.AddMinutes(10)
            };
            await _otpRepo.CreateAsync(entity);

            var fromEmail = _configuration["MailerSend:FromEmail"] ?? string.Empty;
            var fromName = _configuration["MailerSend:FromName"] ?? "QuizUpLearn";
            var mail = new MailerSendEmail
            {
                From = new MailerSendRecipient { Name = fromName, Email = fromEmail },
                Subject = "Password reset OTP",
                Html = $"<p>Your OTP is <strong>{otp}</strong>. It expires in 10 minutes.</p>"
            };
            mail.AddRecipient(account.Email, account.Email);
            await _mailer.SendEmailAsync(mail);
            return true;
        }

        public async Task<bool> VerifyResetPasswordOtpAsync(ResetPasswordVerifyRequestDto dto)
        {
            var email = dto.Email.Trim().ToLowerInvariant();
            var otp = await _otpRepo.GetValidOtpAsync(email, "RESET_PASSWORD", dto.OtpCode.Trim());
            return otp != null;
        }

        public async Task<bool> ConfirmResetPasswordAsync(ResetPasswordConfirmRequestDto dto)
        {
            var email = dto.Email.Trim().ToLowerInvariant();
            var otp = await _otpRepo.GetValidOtpAsync(email, "RESET_PASSWORD", dto.OtpCode.Trim());
            if (otp == null) return false;

            await _otpRepo.MarkUsedAsync(otp.Id);
            var newHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
            var ok = await _accountRepo.UpdatePasswordByEmailAsync(email, newHash);
            return ok;
        }

        public async Task<LoginResponseDto?> LoginWithGoogleAsync(GoogleLoginRequestDto dto)
        {
            try
            {
                // Verify Firebase ID token với Firebase Admin SDK
                FirebaseToken decodedToken;
                try
                {
                    // Firebase Admin SDK sẽ tự động verify:
                    // - Signature với Google's public keys
                    // - Issuer (phải là https://securetoken.google.com/{projectId})
                    // - Audience (phải là projectId)
                    // - Expiration time
                    decodedToken = await FirebaseAuth.DefaultInstance.VerifyIdTokenAsync(dto.IdToken);
                }
                catch (FirebaseAuthException ex)
                {
                    // Token không hợp lệ, đã hết hạn, hoặc không khớp projectId
                    // Có thể do:
                    // 1. Token đã hết hạn
                    // 2. Audience không khớp (projectId sai)
                    // 3. Signature không hợp lệ
                    // 4. Token không phải là Firebase ID token
                    System.Diagnostics.Debug.WriteLine($"Firebase token verification failed: {ex.Message}");
                    return null;
                }
                catch (Exception ex)
                {
                    // Lỗi khác khi verify token (có thể do Firebase chưa được khởi tạo)
                    System.Diagnostics.Debug.WriteLine($"Firebase token verification error: {ex.Message}");
                    return null;
                }
                
                // Lấy email từ decoded token
                var email = decodedToken.Claims.GetValueOrDefault("email")?.ToString();
                if (string.IsNullOrEmpty(email))
                {
                    return null;
                }
                
                // Lấy thông tin khác từ token
                var name = decodedToken.Claims.GetValueOrDefault("name")?.ToString();
                var picture = decodedToken.Claims.GetValueOrDefault("picture")?.ToString();

                email = email.Trim().ToLowerInvariant();
                var account = await _accountRepo.GetByEmailAsync(email);

                // Nếu account chưa tồn tại, tạo mới
                if (account == null)
                {
                    // Tạo account mới với password hash random (vì login bằng Google không cần password)
                    var newAccount = new Account
                    {
                        Email = email,
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString())
                    };
                    account = await _accountRepo.CreateAsync(newAccount);

                    // Tạo subscription miễn phí cho user mới
                    var freePlan = await _subscriptionPlanService.GetFreeSubscriptionPlanAsync();
                    await _subscriptionService.CreateAsync(new RequestSubscriptionDto
                    {
                        UserId = account.UserId,
                        SubscriptionPlanId = freePlan.Id,
                        EndDate = DateTime.UtcNow.AddDays(freePlan.DurationDays)
                    });

                    // Cập nhật thông tin User từ Google
                    var user = await _userRepo.GetByIdAsync(account.UserId);
                    if (user != null)
                    {
                        user.FullName = name ?? string.Empty;
                        user.AvatarUrl = picture ?? string.Empty;
                        await _userRepo.UpdateAsync(user.Id, user);
                    }
                }
                else
                {
                    // Account đã tồn tại, kiểm tra trạng thái
                    if (account.DeletedAt != null || !account.IsActive)
                    {
                        return null;
                    }

                    // Cập nhật thông tin User từ Google nếu có thay đổi
                    var user = await _userRepo.GetByIdAsync(account.UserId);
                    if (user != null)
                    {
                        var updated = false;
                        if (!string.IsNullOrEmpty(name) && user.FullName != name)
                        {
                            user.FullName = name;
                            updated = true;
                        }
                        
                        // Chỉ cập nhật avatar từ Google nếu:
                        // 1. User chưa có avatar (rỗng)
                        // 2. Hoặc avatar hiện tại là Google avatar (chứa googleusercontent.com)
                        // → Không ghi đè avatar custom của user
                        if (!string.IsNullOrEmpty(picture))
                        {
                            var isGoogleAvatar = !string.IsNullOrEmpty(user.AvatarUrl) && 
                                                (user.AvatarUrl.Contains("googleusercontent.com") || 
                                                 user.AvatarUrl.Contains("googleapis.com"));
                            var isEmptyAvatar = string.IsNullOrEmpty(user.AvatarUrl) || user.AvatarUrl == string.Empty;
                            
                            if (isEmptyAvatar || isGoogleAvatar)
                            {
                                if (user.AvatarUrl != picture)
                                {
                                    user.AvatarUrl = picture;
                                    updated = true;
                                }
                            }
                            // Nếu user đã có avatar custom (không phải Google), giữ nguyên
                        }
                        
                        if (updated)
                        {
                            await _userRepo.UpdateAsync(user.Id, user);
                        }
                    }
                }

                // Cập nhật LastLoginAt và IsEmailVerified
                account.LastLoginAt = DateTime.UtcNow;
                account.IsEmailVerified = true; // Google email đã được verify
                await _accountRepo.UpdateAsync(account.Id, account);

                // Reload account với User và Role để map đúng
                account = await _accountRepo.GetByEmailAsync(email);
                if (account == null) return null;

                return new LoginResponseDto
                {
                    Account = _mapper.Map<ResponseAccountDto>(account),
                    AccessToken = string.Empty,
                    ExpiresAt = DateTime.UtcNow,
                    RefreshToken = string.Empty,
                    RefreshExpiresAt = DateTime.UtcNow
                };
            }
            catch (FirebaseAuthException)
            {
                // Token không hợp lệ (đã được xử lý ở trên)
                return null;
            }
            catch (ArgumentException)
            {
                // Lỗi cấu hình
                throw;
            }
            catch (Exception ex)
            {
                // Lỗi khác
                System.Diagnostics.Debug.WriteLine($"Unexpected error in LoginWithGoogleAsync: {ex.Message}");
                return null;
            }
        }

        public (string token, DateTime expiresAt) GenerateJwtToken(ResponseAccountDto account)
        {
            var jwt = _configuration.GetSection("Jwt");
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["Key"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expires = DateTime.UtcNow.AddMinutes(int.Parse(jwt["ExpiresMinutes"]!));
            var issuer = jwt["Issuer"]?.Trim() ?? throw new InvalidOperationException("JWT Issuer is not configured");
            var audience = jwt["Audience"]?.Trim() ?? throw new InvalidOperationException("JWT Audience is not configured");

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, account.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, account.Email ?? string.Empty),
                new Claim("userId", account.UserId.ToString()),
                new Claim("roleId", account.RoleId.ToString()),
                new Claim("roleName", account.RoleName ?? string.Empty)
            };

            // Thêm exp claim một cách rõ ràng để đảm bảo có trong payload
            var now = DateTime.UtcNow;
            var expClaim = new Claim(JwtRegisteredClaimNames.Exp,
                new DateTimeOffset(expires).ToUnixTimeSeconds().ToString(),
                ClaimValueTypes.Integer64);
            claims.Add(expClaim);

            // Thêm nbf và iat claims
            var nbfClaim = new Claim(JwtRegisteredClaimNames.Nbf,
                new DateTimeOffset(now).ToUnixTimeSeconds().ToString(),
                ClaimValueTypes.Integer64);
            var iatClaim = new Claim(JwtRegisteredClaimNames.Iat,
                new DateTimeOffset(now).ToUnixTimeSeconds().ToString(),
                ClaimValueTypes.Integer64);
            claims.Add(nbfClaim);
            claims.Add(iatClaim);

            var tokenHandler = new JwtSecurityTokenHandler();
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                NotBefore = now,
                IssuedAt = now,
                Expires = expires,
                Issuer = issuer,
                Audience = audience,
                SigningCredentials = creds
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);

            return (tokenHandler.WriteToken(token), expires);
        }

        private static string GenerateSixDigitOtp()
        {
            var random = new Random();
            return random.Next(0, 1000000).ToString("D6");
        }

        public string GenerateRefreshToken()
        {
            return Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(64));
        }

        public async Task SaveRefreshTokenAsync(Guid accountId, string refreshToken)
        {
            var key = $"refresh:{accountId}:{refreshToken}";
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(30)
            };
            await _cache.SetStringAsync(key, "1", options);
        }

        public async Task<bool> ValidateRefreshTokenAsync(Guid accountId, string refreshToken)
        {
            var key = $"refresh:{accountId}:{refreshToken}";
            var value = await _cache.GetStringAsync(key);
            return !string.IsNullOrEmpty(value);
        }

        public async Task DeleteRefreshTokenAsync(Guid accountId, string refreshToken)
        {
            var key = $"refresh:{accountId}:{refreshToken}";
            await _cache.RemoveAsync(key);
        }
    }
}


