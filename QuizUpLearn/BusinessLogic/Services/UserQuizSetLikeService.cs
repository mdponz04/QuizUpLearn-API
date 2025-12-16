using AutoMapper;
using BusinessLogic.DTOs;
using BusinessLogic.DTOs.UserQuizSetLikeDtos;
using BusinessLogic.Extensions;
using BusinessLogic.Interfaces;
using Repository.Entities;
using Repository.Interfaces;

namespace BusinessLogic.Services
{
    public class UserQuizSetLikeService : IUserQuizSetLikeService
    {
        private readonly IUserQuizSetLikeRepo _userQuizSetLikeRepo;
        private readonly IMapper _mapper;

        public UserQuizSetLikeService(IUserQuizSetLikeRepo userQuizSetLikeRepo, IMapper mapper)
        {
            _userQuizSetLikeRepo = userQuizSetLikeRepo;
            _mapper = mapper;
        }

        public async Task<ResponseUserQuizSetLikeDto> CreateAsync(RequestUserQuizSetLikeDto dto)
        {
            var exists = await _userQuizSetLikeRepo.IsExistAsync(dto.UserId, dto.QuizSetId);
            if (exists)
                throw new InvalidOperationException("User has already liked this quiz set");

            var entity = _mapper.Map<UserQuizSetLike>(dto);
            var created = await _userQuizSetLikeRepo.CreateAsync(entity);
            return _mapper.Map<ResponseUserQuizSetLikeDto>(created);
        }

        public async Task<ResponseUserQuizSetLikeDto?> GetByIdAsync(Guid id)
        {
            var entity = await _userQuizSetLikeRepo.GetByIdAsync(id);
            return entity == null ? null : _mapper.Map<ResponseUserQuizSetLikeDto>(entity);
        }

        public async Task<PaginationResponseDto<ResponseUserQuizSetLikeDto>> GetAllAsync(PaginationRequestDto pagination, bool includeDeleted = false)
        {
            var entities = await _userQuizSetLikeRepo.GetAllAsync(includeDeleted);
            var dtos = _mapper.Map<IEnumerable<ResponseUserQuizSetLikeDto>>(entities);
            return dtos.ToPagedResponse(pagination);
        }

        public async Task<PaginationResponseDto<ResponseUserQuizSetLikeDto>> GetByUserIdAsync(Guid userId, PaginationRequestDto pagination, bool includeDeleted = false)
        {
            var entities = await _userQuizSetLikeRepo.GetByUserIdAsync(userId, includeDeleted);
            var dtos = _mapper.Map<IEnumerable<ResponseUserQuizSetLikeDto>>(entities);
            return dtos.ToPagedResponse(pagination);
        }

        public async Task<PaginationResponseDto<ResponseUserQuizSetLikeDto>> GetByQuizSetIdAsync(Guid quizSetId, PaginationRequestDto pagination, bool includeDeleted = false)
        {
            var entities = await _userQuizSetLikeRepo.GetByQuizSetIdAsync(quizSetId, includeDeleted);
            var dtos = _mapper.Map<IEnumerable<ResponseUserQuizSetLikeDto>>(entities);
            return dtos.ToPagedResponse(pagination);
        }

        public async Task<ResponseUserQuizSetLikeDto?> GetByUserAndQuizSetAsync(Guid userId, Guid quizSetId, bool includeDeleted = false)
        {
            var entity = await _userQuizSetLikeRepo.GetByUserAndQuizSetAsync(userId, quizSetId, includeDeleted);
            return entity == null ? null : _mapper.Map<ResponseUserQuizSetLikeDto>(entity);
        }

        public async Task<bool> ToggleLikeAsync(Guid userId, Guid quizSetId)
        {
            var existing = await _userQuizSetLikeRepo.GetByUserAndQuizSetAsync(userId, quizSetId);
            if (existing != null)
            {
                // Remove like
                return await _userQuizSetLikeRepo.HardDeleteAsync(existing.Id);
            }
            else
            {
                // Add like
                var entity = new UserQuizSetLike
                {
                    UserId = userId,
                    QuizSetId = quizSetId
                };
                await _userQuizSetLikeRepo.CreateAsync(entity);
                return true;
            }
        }

        public async Task<bool> HardDeleteAsync(Guid id)
        {
            return await _userQuizSetLikeRepo.HardDeleteAsync(id);
        }

        public async Task<bool> IsExistAsync(Guid userId, Guid quizSetId)
        {
            return await _userQuizSetLikeRepo.IsExistAsync(userId, quizSetId);
        }

        public async Task<int> GetLikeCountByQuizSetAsync(Guid quizSetId)
        {
            return await _userQuizSetLikeRepo.GetLikeCountByQuizSetAsync(quizSetId);
        }
    }
}
