using AutoMapper;
using BusinessLogic.DTOs.UserMistakeDtos;
using BusinessLogic.Interfaces;
using Repository.Entities;
using Repository.Interfaces;
using BusinessLogic.Extensions;
using BusinessLogic.DTOs;

namespace BusinessLogic.Services
{
    public class UserMistakeService : IUserMistakeService
    {
        private readonly IUserMistakeRepo _repo;
        private readonly IMapper _mapper;

        public UserMistakeService(IUserMistakeRepo repo, IMapper mapper)
        {
            _repo = repo;
            _mapper = mapper;
        }

        public async Task<PaginationResponseDto<ResponseUserMistakeDto>> GetAllAsync(PaginationRequestDto pagination = null!)
        {
            pagination ??= new PaginationRequestDto();
            var userMistakes = await _repo.GetAllAsync();
            var dtos = _mapper.Map<IEnumerable<ResponseUserMistakeDto>>(userMistakes);
            return dtos.ToPagedResponse(pagination);
        }

        public async Task<ResponseUserMistakeDto?> GetByIdAsync(Guid id)
        {
            var userMistake = await _repo.GetByIdAsync(id);
            return userMistake != null ? _mapper.Map<ResponseUserMistakeDto>(userMistake) : null;
        }

        public async Task AddAsync(RequestUserMistakeDto requestDto)
        {
            var userMistake = _mapper.Map<UserMistake>(requestDto);
            await _repo.AddAsync(userMistake);
        }

        public async Task UpdateAsync(Guid id, RequestUserMistakeDto requestDto)
        {
            await _repo.UpdateAsync(id, _mapper.Map<UserMistake>(requestDto));
        }

        public async Task DeleteAsync(Guid id)
        {
            await _repo.DeleteAsync(id);
        }

        public async Task<PaginationResponseDto<ResponseUserMistakeDto>> GetAllByUserIdAsync(Guid userId, PaginationRequestDto pagination = null!)
        {
            pagination ??= new PaginationRequestDto();
            var userMistakes = await _repo.GetAlByUserIdAsync(userId);
            var dtos = _mapper.Map<IEnumerable<ResponseUserMistakeDto>>(userMistakes);
            return dtos.ToPagedResponse(pagination);
        }
    }
}
