using AutoMapper;
using BusinessLogic.DTOs.UserWeakPointDtos;
using BusinessLogic.Interfaces;
using BusinessLogic.Extensions;
using Repository.Entities;
using Repository.Interfaces;
using BusinessLogic.DTOs;

namespace BusinessLogic.Services
{
    public class UserWeakPointService : IUserWeakPointService
    {
        private readonly IUserWeakPointRepo _repo;
        private readonly IMapper _mapper;

        public UserWeakPointService(IUserWeakPointRepo repo, IMapper mapper)
        {
            _repo = repo;
            _mapper = mapper;
        }

        public async Task<PaginationResponseDto<ResponseUserWeakPointDto>> GetByUserIdAsync(Guid userId, PaginationRequestDto pagination = null!)
        {
            pagination ??= new PaginationRequestDto();
            var entities = await _repo.GetByUserIdAsync(userId);
            var dtos = _mapper.Map<IEnumerable<ResponseUserWeakPointDto>>(entities);
            return dtos.ToPagedResponse(pagination);
        }

        public async Task<ResponseUserWeakPointDto?> GetByIdAsync(Guid id)
        {
            var entity = await _repo.GetByIdAsync(id);
            return entity == null ? null : _mapper.Map<ResponseUserWeakPointDto>(entity);
        }

        public async Task<ResponseUserWeakPointDto?> AddAsync(RequestUserWeakPointDto dto)
        {
            var entity = _mapper.Map<UserWeakPoint>(dto);
            var result = await _repo.AddAsync(entity);
            return result == null ? null : _mapper.Map<ResponseUserWeakPointDto>(result);
        }

        public async Task<ResponseUserWeakPointDto?> UpdateAsync(Guid id, RequestUserWeakPointDto dto)
        {
            var entity = _mapper.Map<UserWeakPoint>(dto);
            var result = await _repo.UpdateAsync(id, entity);
            return result == null ? null : _mapper.Map<ResponseUserWeakPointDto>(result);
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            return await _repo.DeleteAsync(id);
        }

        public async Task<bool> IsWeakPointExistedAsync(string weakPoint, Guid userId)
        {
            return await _repo.IsWeakPointExisted(weakPoint, userId);
        }
    }
}
