using AutoMapper;
using BusinessLogic.DTOs;
using BusinessLogic.DTOs.UserReportDtos;
using BusinessLogic.Extensions;
using BusinessLogic.Interfaces;
using Repository.Entities;
using Repository.Interfaces;

namespace BusinessLogic.Services
{
    public class UserReportService : IUserReportService
    {
        private readonly IUserReportRepo _userReportRepo;
        private readonly IMapper _mapper;

        public UserReportService(IUserReportRepo userReportRepo, IMapper mapper)
        {
            _userReportRepo = userReportRepo;
            _mapper = mapper;
        }

        public async Task<ResponseUserReportDto> CreateAsync(RequestUserReportDto dto)
        {
            if(dto == null)
                throw new ArgumentNullException("DTO cannot be null");
            var entity = _mapper.Map<UserReport>(dto);
            var created = await _userReportRepo.CreateAsync(entity);
            return _mapper.Map<ResponseUserReportDto>(created);
        }

        public async Task<ResponseUserReportDto?> GetByIdAsync(Guid id)
        {
            var entity = await _userReportRepo.GetByIdAsync(id);
            return entity == null ? null : _mapper.Map<ResponseUserReportDto>(entity);
        }

        public async Task<PaginationResponseDto<ResponseUserReportDto>> GetAllAsync(PaginationRequestDto pagination, bool includeDeleted = false)
        {
            var entities = await _userReportRepo.GetAllAsync(includeDeleted);
            var dtos = _mapper.Map<IEnumerable<ResponseUserReportDto>>(entities);
            return dtos.ToPagedResponse(pagination);
        }

        public async Task<PaginationResponseDto<ResponseUserReportDto>> GetByUserIdAsync(Guid userId, PaginationRequestDto pagination, bool includeDeleted = false)
        {
            var entities = await _userReportRepo.GetByUserIdAsync(userId, includeDeleted);
            var dtos = _mapper.Map<IEnumerable<ResponseUserReportDto>>(entities);
            return dtos.ToPagedResponse(pagination);
        }

        public async Task<bool> HardDeleteAsync(Guid id)
        {
            if(id == Guid.Empty)
                throw new Exception("Invalid ID");
            return await _userReportRepo.HardDeleteAsync(id);
        }

        public async Task<bool> IsExistAsync(Guid userId)
        {
            if(userId == Guid.Empty)
                throw new Exception("Invalid user id");
            return await _userReportRepo.IsExistAsync(userId);
        }
    }
}


