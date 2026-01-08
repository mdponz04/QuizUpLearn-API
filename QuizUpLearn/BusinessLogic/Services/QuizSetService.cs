using AutoMapper;
using BusinessLogic.DTOs;
using BusinessLogic.DTOs.QuizSetDtos;
using BusinessLogic.Helpers;
using BusinessLogic.Interfaces;
using Repository.Entities;
using Repository.Enums;
using Repository.Interfaces;

namespace BusinessLogic.Services
{
    public class QuizSetService : IQuizSetService
    {
        private readonly IQuizSetRepo _quizSetRepo;
        private readonly IMapper _mapper;

        public QuizSetService(IQuizSetRepo quizSetRepo, IMapper mapper)
        {
            _quizSetRepo = quizSetRepo;
            _mapper = mapper;
        }

        public async Task<QuizSetResponseDto> CreateQuizSetAsync(QuizSetRequestDto quizSetDto)
        {
            if(quizSetDto.CreatedBy == null || quizSetDto.CreatedBy == Guid.Empty)
            {
                throw new ArgumentException("CreatedBy cannot be null or empty");
            }
            var quizSet = _mapper.Map<QuizSet>(quizSetDto);
            var createdQuizSet = await _quizSetRepo.CreateQuizSetAsync(quizSet);
            return _mapper.Map<QuizSetResponseDto>(createdQuizSet);
        }

        public async Task<QuizSetResponseDto> GetQuizSetByIdAsync(Guid id)
        {
            var quizSet = await _quizSetRepo.GetQuizSetByIdAsync(id);
            return _mapper.Map<QuizSetResponseDto>(quizSet);
        }

        public async Task<PaginationResponseDto<QuizSetResponseDto>> GetAllQuizSetsAsync(PaginationRequestDto pagination)
        {
            var filters = ExtractFilterValues(pagination);

            var quizSets = await _quizSetRepo.GetAllQuizSetsAsync();

            // Apply search engine (filters, search, sorting)
            var filtered = ApplyFilters(quizSets.AsQueryable(), filters.isDeleted, filters.isPremiumOnly, filters.isPublished, filters.quizSetType);
            var searched = ApplySearch(filtered, pagination.SearchTerm);
            var sorted = ApplySorting(searched, pagination.SortBy, pagination.GetNormalizedSortDirection());

            var dtos = _mapper.Map<IEnumerable<QuizSetResponseDto>>(sorted);
            return PaginationHelper.CreatePagedResponse(dtos, pagination);
        }

        public async Task<PaginationResponseDto<QuizSetResponseDto>> GetQuizSetsByCreatorAsync(Guid creatorId, PaginationRequestDto pagination)
        {
            var filters = ExtractFilterValues(pagination);

            var quizSets = await _quizSetRepo.GetQuizSetsByCreatorAsync(creatorId);

            // Apply search engine (filters, search, sorting)
            var filtered = ApplyFilters(quizSets.AsQueryable(), filters.isDeleted, filters.isPremiumOnly, filters.isPublished, filters.quizSetType);
            var searched = ApplySearch(filtered, pagination.SearchTerm);
            var sorted = ApplySorting(searched, pagination.SortBy, pagination.GetNormalizedSortDirection());

            var dtos = _mapper.Map<IEnumerable<QuizSetResponseDto>>(sorted);
            return PaginationHelper.CreatePagedResponse(dtos, pagination);
        }

        public async Task<PaginationResponseDto<QuizSetResponseDto>> GetPublishedQuizSetsAsync(PaginationRequestDto pagination)
        {
            var filters = ExtractFilterValues(pagination);

            var quizSets = await _quizSetRepo.GetPublishedQuizSetsAsync();

            // Apply search engine (filters, search, sorting)
            var filtered = ApplyFilters(quizSets.AsQueryable(), null, filters.isPremiumOnly, true, filters.quizSetType);
            var searched = ApplySearch(filtered, pagination.SearchTerm);
            var sorted = ApplySorting(searched, pagination.SortBy, pagination.GetNormalizedSortDirection());

            var dtos = _mapper.Map<IEnumerable<QuizSetResponseDto>>(sorted);
            return PaginationHelper.CreatePagedResponse(dtos, pagination);
        }

        public async Task<QuizSetResponseDto> UpdateQuizSetAsync(Guid id, QuizSetRequestDto quizSetDto)
        {
            var updatedQuizSet = await _quizSetRepo.UpdateQuizSetAsync(id, _mapper.Map<QuizSet>(quizSetDto));
            return _mapper.Map<QuizSetResponseDto>(updatedQuizSet);
        }

        public async Task<bool> SoftDeleteQuizSetAsync(Guid id)
        {
            return await _quizSetRepo.SoftDeleteQuizSetAsync(id);
        }

        public async Task<bool> HardDeleteQuizSetAsync(Guid id)
        {
            return await _quizSetRepo.HardDeleteQuizSetAsync(id);
        }

        public async Task<QuizSetResponseDto> RestoreQuizSetAsync(Guid id)
        {
            var quizSet = await _quizSetRepo.RestoreQuizSetAsync(id);
            return _mapper.Map<QuizSetResponseDto>(quizSet);
        }

        public async Task<bool> RequestValidateByModAsync(Guid id)
        {
            return await _quizSetRepo.RequestValidateByModAsync(id);
        }

        public async Task<bool> ValidateQuizSetAsync(Guid id)
        {
            return await _quizSetRepo.ValidateQuizSetAsync(id);
        }

        private IQueryable<QuizSet> ApplyFilters(
            IQueryable<QuizSet> query,
            bool? isDeleted = null,
            bool? isPremiumOnly = null,
            bool? isPublished = null,
            QuizSetTypeEnum? quizSetType = null)
        {
            if (isDeleted.HasValue)
            {
                if (isDeleted.Value)
                {
                    query = query.Where(qs => qs.DeletedAt != null);
                }
                else
                {
                    query = query.Where(qs => qs.DeletedAt == null);
                }
            }
            else
            {
                query = query.Where(qs => qs.DeletedAt == null);
            }

            if (isPremiumOnly.HasValue)
            {
                query = query.Where(qs => qs.IsPremiumOnly == isPremiumOnly.Value);
            }

            if (isPublished.HasValue)
            {
                query = query.Where(qs => qs.IsPublished == isPublished.Value);
            }

            if (quizSetType.HasValue)
            {
                query = query.Where(qs => qs.QuizSetType == quizSetType.Value);
            }
            return query;
        }

        private IQueryable<QuizSet> ApplySearch(IQueryable<QuizSet> query, string? searchTerm)
        {
            if (string.IsNullOrEmpty(searchTerm))
                return query;

            var normalizedSearchTerm = searchTerm.ToLower();

            return query.Where(qs =>
                (qs.Title != null && qs.Title.ToLower().Contains(normalizedSearchTerm)) ||
                (qs.Description != null && qs.Description.ToLower().Contains(normalizedSearchTerm))
            );
        }

        private IQueryable<QuizSet> ApplySorting(IQueryable<QuizSet> query, string? sortBy, string? sortDirection)
        {
            if (string.IsNullOrEmpty(sortBy))
                return query.OrderByDescending(qs => qs.CreatedAt);

            var isDescending = !string.IsNullOrEmpty(sortDirection) &&
                              sortDirection.Equals("desc", StringComparison.OrdinalIgnoreCase);

            return sortBy.ToLower() switch
            {
                "title" => isDescending
                    ? query.OrderByDescending(qs => qs.Title)
                    : query.OrderBy(qs => qs.Title),
                "createdat" => isDescending
                    ? query.OrderByDescending(qs => qs.CreatedAt)
                    : query.OrderBy(qs => qs.CreatedAt),
                "updatedat" => isDescending
                    ? query.OrderByDescending(qs => qs.UpdatedAt)
                    : query.OrderBy(qs => qs.UpdatedAt),
                "deletedat" => isDescending
                    ? query.OrderByDescending(qs => qs.DeletedAt)
                    : query.OrderBy(qs => qs.DeletedAt),
                "totalattempts" => isDescending
                    ? query.OrderByDescending(qs => qs.TotalAttempts)
                    : query.OrderBy(qs => qs.TotalAttempts),
                "averagescore" => isDescending
                    ? query.OrderByDescending(qs => qs.AverageScore)
                    : query.OrderBy(qs => qs.AverageScore),
                "quizsettype" => isDescending
                    ? query.OrderByDescending(qs => qs.QuizSetType)
                    : query.OrderBy(qs => qs.QuizSetType),
                _ => query.OrderByDescending(qs => qs.CreatedAt)
            };
        }

        private (bool? isDeleted, bool? isPremiumOnly, bool? isPublished, QuizSetTypeEnum? quizSetType) ExtractFilterValues(PaginationRequestDto pagination)
        {
            var jsonExtractHelper = new JsonExtractHelper();
            if (pagination.Filters == null)
                return (null, null, null, null);

            bool? showDeleted = jsonExtractHelper.GetBoolFromFilter(pagination.Filters, "isDeleted");
            bool? showPremiumOnly = jsonExtractHelper.GetBoolFromFilter(pagination.Filters, "isPremiumOnly");
            bool? showPublished = jsonExtractHelper.GetBoolFromFilter(pagination.Filters, "isPublished");
            QuizSetTypeEnum? quizSetType = jsonExtractHelper.GetEnumFromFilter(pagination.Filters, "quizSetType");

            return (showDeleted, showPremiumOnly, showPublished, quizSetType);
        }
    }
}
