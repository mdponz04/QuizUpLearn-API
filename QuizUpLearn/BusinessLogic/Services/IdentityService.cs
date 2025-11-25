using AutoMapper;
using BusinessLogic.DTOs;
using BusinessLogic.Interfaces;
using BusinessLogic.DTOs.SubscriptionDtos;
using Repository.Entities;
using Repository.Interfaces;
using BCrypt.Net;
using Repository.Models;
using Microsoft.Extensions.Configuration;

namespace BusinessLogic.Services
{
    public class IdentityService : IIdentityService
    {
        private readonly IAccountRepo _accountRepo;
        private readonly IMapper _mapper;
        private readonly IOtpVerificationRepo _otpRepo;
        private readonly IMailerSendService _mailer;
        private readonly IConfiguration _configuration;
        private readonly ISubscriptionService _subscriptionService;
        private readonly ISubscriptionPlanService _subscriptionPlanService;

        public IdentityService(
            IAccountRepo accountRepo,
            IOtpVerificationRepo otpRepo,
            IMailerSendService mailer,
            IConfiguration configuration,
            IMapper mapper,
            ISubscriptionService subscriptionService,
            ISubscriptionPlanService subscriptionPlanService)
        {
            _accountRepo = accountRepo;
            _otpRepo = otpRepo;
            _mailer = mailer;
            _configuration = configuration;
            _mapper = mapper;
            _subscriptionService = subscriptionService;
            _subscriptionPlanService = subscriptionPlanService;
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
                AiGenerateQuizSetRemaining = freePlan.AiGenerateQuizSetMaxTimes,
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

        private static string GenerateSixDigitOtp()
        {
            var random = new Random();
            return random.Next(0, 1000000).ToString("D6");
        }
    }
}


