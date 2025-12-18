using AutoMapper;
using BusinessLogic.DTOs;
using BusinessLogic.DTOs.UserQuizSetFavoriteDtos;
using BusinessLogic.Extensions;
using BusinessLogic.Interfaces;
using Repository.Entities;
using Repository.Interfaces;

namespace BusinessLogic.Services
{
    public class UserQuizSetFavoriteService : IUserQuizSetFavoriteService
    {
        private readonly IUserQuizSetFavoriteRepo _userQuizSetFavoriteRepo;
        private readonly IMapper _mapper;

        public UserQuizSetFavoriteService(IUserQuizSetFavoriteRepo userQuizSetFavoriteRepo, IMapper mapper)
        {
            _userQuizSetFavoriteRepo = userQuizSetFavoriteRepo;
            _mapper = mapper;
        }

        public async Task<ResponseUserQuizSetFavoriteDto> CreateAsync(RequestUserQuizSetFavoriteDto dto)
        {
            var exists = await _userQuizSetFavoriteRepo.IsExistAsync(dto.UserId, dto.QuizSetId);
            if (exists)
                throw new InvalidOperationException("User has already favorited this quiz set");

            var entity = _mapper.Map<UserQuizSetFavorite>(dto);
            var created = await _userQuizSetFavoriteRepo.CreateAsync(entity);
            return _mapper.Map<ResponseUserQuizSetFavoriteDto>(created);
        }

        public async Task<ResponseUserQuizSetFavoriteDto?> GetByIdAsync(Guid id)
        {
            var entity = await _userQuizSetFavoriteRepo.GetByIdAsync(id);
            return entity == null ? null : _mapper.Map<ResponseUserQuizSetFavoriteDto>(entity);
        }

        public async Task<PaginationResponseDto<ResponseUserQuizSetFavoriteDto>> GetAllAsync(PaginationRequestDto pagination, bool includeDeleted = false)
        {
            var entities = await _userQuizSetFavoriteRepo.GetAllAsync(includeDeleted);
            var query = entities.AsQueryable();

            query = ApplySearch(query, pagination.SearchTerm);
            query = ApplySortOrder(query, pagination.SortBy, pagination.GetNormalizedSortDirection());

            var dtos = _mapper.Map<IEnumerable<ResponseUserQuizSetFavoriteDto>>(query.ToList());
            return dtos.ToPagedResponse(pagination);
        }

        public async Task<PaginationResponseDto<ResponseUserQuizSetFavoriteDto>> GetByUserIdAsync(Guid userId, PaginationRequestDto pagination, bool includeDeleted = false)
        {
            var entities = await _userQuizSetFavoriteRepo.GetByUserIdAsync(userId, includeDeleted);
            var query = entities.AsQueryable();

            query = ApplySearch(query, pagination.SearchTerm);
            query = ApplySortOrder(query, pagination.SortBy, pagination.GetNormalizedSortDirection());

            var dtos = _mapper.Map<IEnumerable<ResponseUserQuizSetFavoriteDto>>(query.ToList());
            return dtos.ToPagedResponse(pagination);
        }

        public async Task<PaginationResponseDto<ResponseUserQuizSetFavoriteDto>> GetByQuizSetIdAsync(Guid quizSetId, PaginationRequestDto pagination, bool includeDeleted = false)
        {
            var entities = await _userQuizSetFavoriteRepo.GetByQuizSetIdAsync(quizSetId, includeDeleted);
            var query = entities.AsQueryable();

            query = ApplySearch(query, pagination.SearchTerm);
            query = ApplySortOrder(query, pagination.SortBy, pagination.GetNormalizedSortDirection());

            var dtos = _mapper.Map<IEnumerable<ResponseUserQuizSetFavoriteDto>>(query.ToList());
            return dtos.ToPagedResponse(pagination);
        }

        public async Task<ResponseUserQuizSetFavoriteDto?> GetByUserAndQuizSetAsync(Guid userId, Guid quizSetId, bool includeDeleted = false)
        {
            var entity = await _userQuizSetFavoriteRepo.GetByUserAndQuizSetAsync(userId, quizSetId, includeDeleted);
            return entity == null ? null : _mapper.Map<ResponseUserQuizSetFavoriteDto>(entity);
        }

        public async Task<bool> ToggleFavoriteAsync(Guid userId, Guid quizSetId)
        {
            var existing = await _userQuizSetFavoriteRepo.GetByUserAndQuizSetAsync(userId, quizSetId);
            if (existing != null)
            {
                // Remove favorite
                return await _userQuizSetFavoriteRepo.HardDeleteAsync(existing.Id);
            }
            else
            {
                // Add favorite
                var entity = new UserQuizSetFavorite
                {
                    UserId = userId,
                    QuizSetId = quizSetId
                };
                await _userQuizSetFavoriteRepo.CreateAsync(entity);
                return true;
            }
        }

        public async Task<bool> HardDeleteAsync(Guid id)
        {
            return await _userQuizSetFavoriteRepo.HardDeleteAsync(id);
        }

        public async Task<bool> IsExistAsync(Guid userId, Guid quizSetId)
        {
            return await _userQuizSetFavoriteRepo.IsExistAsync(userId, quizSetId);
        }

        public async Task<int> GetFavoriteCountByQuizSetAsync(Guid quizSetId)
        {
            return await _userQuizSetFavoriteRepo.GetFavoriteCountByQuizSetAsync(quizSetId);
        }

        private static IQueryable<UserQuizSetFavorite> ApplySearch(IQueryable<UserQuizSetFavorite> query, string? searchTerm)
        {
            if (string.IsNullOrEmpty(searchTerm))
                return query;

            var normalizedSearchTerm = searchTerm.ToLower();

            query = query.Where(uqsf => uqsf.QuizSet != null
            && !string.IsNullOrEmpty(uqsf.QuizSet.Title)
            && uqsf.QuizSet.Title.ToLower().Contains(normalizedSearchTerm));

            return query;
        }
        private static IQueryable<UserQuizSetFavorite> ApplySortOrder(IQueryable<UserQuizSetFavorite> query, string? sortBy, string sortDirection)
        {
            var sortField = sortBy ?? "CreatedAt";

            return sortField.ToLower() switch
            {
                "createdat" => sortDirection == "desc"
                    ? query.OrderByDescending(q => q.CreatedAt)
                    : query.OrderBy(q => q.CreatedAt),
                _ => query.OrderByDescending(q => q.CreatedAt)
            };
        }
    }
}
