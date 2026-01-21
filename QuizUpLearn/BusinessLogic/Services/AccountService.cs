using AutoMapper;
using BusinessLogic.DTOs;
using BusinessLogic.Extensions;
using BusinessLogic.Interfaces;
using Repository.Entities;
using Repository.Interfaces;
using BCrypt.Net;

namespace BusinessLogic.Services
{
    public class AccountService : IAccountService
    {
        private readonly IAccountRepo _repo;
        private readonly IMapper _mapper;

        public AccountService(IAccountRepo repo, IMapper mapper)
        {
            _repo = repo;
            _mapper = mapper;
        }

        public async Task<ResponseAccountDto> CreateAsync(RequestAccountDto dto)
        {
            // Hash password trước khi map sang entity
            var entity = _mapper.Map<Account>(dto);
            entity.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);
            var created = await _repo.CreateAsync(entity);
            return _mapper.Map<ResponseAccountDto>(created);
        }

        public async Task<IEnumerable<ResponseAccountDto>> GetAllAsync(bool includeDeleted = false)
        {
            var list = await _repo.GetAllAsync(includeDeleted);
            return _mapper.Map<IEnumerable<ResponseAccountDto>>(list);
        }

        public async Task<PaginationResponseDto<ResponseAccountDto>> GetAllAsync(PaginationRequestDto pagination, bool includeDeleted = false)
        {
            var entities = await _repo.GetAllAsync(includeDeleted);
            var dtos = _mapper.Map<IEnumerable<ResponseAccountDto>>(entities);
            return dtos.ToPagedResponse(pagination);
        }

        public async Task<ResponseAccountDto?> GetByIdAsync(Guid id)
        {
            var entity = await _repo.GetByIdAsync(id);
            return entity == null ? null : _mapper.Map<ResponseAccountDto>(entity);
        }

        public async Task<bool> RestoreAsync(Guid id)
        {
            return await _repo.RestoreAsync(id);
        }

        public async Task<bool> SoftDeleteAsync(Guid id)
        {
            return await _repo.SoftDeleteAsync(id);
        }

        public async Task<ResponseAccountDto?> UpdateAsync(Guid id, RequestAccountDto dto)
        {
            var existingAccount = await _repo.GetByIdAsync(id);
            if (existingAccount == null) return null;

            var entity = _mapper.Map<Account>(dto);
            
            // Chỉ hash và update password nếu có password mới
            if (!string.IsNullOrWhiteSpace(dto.Password))
            {
                entity.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);
            }
            else
            {
                // Giữ nguyên password cũ
                entity.PasswordHash = existingAccount.PasswordHash;
            }
            
            var updated = await _repo.UpdateAsync(id, entity);
            return updated == null ? null : _mapper.Map<ResponseAccountDto>(updated);
        }
    }
}


