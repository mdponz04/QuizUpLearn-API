using AutoMapper;
using BusinessLogic.DTOs;
using BusinessLogic.Interfaces;
using Repository.Entities;
using Repository.Interfaces;
using BCrypt.Net;

namespace BusinessLogic.Services
{
    public class IdentityService : IIdentityService
    {
        private readonly IAccountRepo _accountRepo;
        private readonly IMapper _mapper;

        public IdentityService(IAccountRepo accountRepo, IMapper mapper)
        {
            _accountRepo = accountRepo;
            _mapper = mapper;
        }

        public async Task<LoginResponseDto?> LoginAsync(LoginRequestDto dto)
        {
            var email = dto.Email.Trim().ToLowerInvariant();
            var account = await _accountRepo.GetByEmailAsync(email);
            if (account == null) return null;
            if (!BCrypt.Net.BCrypt.Verify(dto.Password, account.PasswordHash)) return null;
            if (account.DeletedAt != null || !account.IsActive) return null;

            account.LastLoginAt = DateTime.Now;
            await _accountRepo.UpdateAsync(account.Id, account);
            // Token sẽ được tạo ở Controller, service chỉ trả về Account (đơn giản hóa theo yêu cầu)
            return new LoginResponseDto
            {
                Account = _mapper.Map<ResponseAccountDto>(account),
                AccessToken = string.Empty,
                ExpiresAt = DateTime.Now
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
            return _mapper.Map<ResponseAccountDto>(created);
        }
    }
}


