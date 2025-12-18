using AutoMapper;
using BusinessLogic.DTOs;
using BusinessLogic.DTOs.QuizDtos;
using BusinessLogic.Helpers;
using BusinessLogic.Interfaces;
using Repository.Entities;
using Repository.Interfaces;

namespace BusinessLogic.Services
{
    public class QuizService : IQuizService
    {
        private readonly IQuizRepo _quizRepo;
        private readonly IMapper _mapper;

        public QuizService(IQuizRepo quizRepo, IMapper mapper)
        {
            _quizRepo = quizRepo;
            _mapper = mapper;
        }

        public async Task<QuizResponseDto> CreateQuizAsync(QuizRequestDto quizDto)
        {
            var quiz = _mapper.Map<Quiz>(quizDto);
            var createdQuiz = await _quizRepo.CreateQuizAsync(quiz);
            return _mapper.Map<QuizResponseDto>(createdQuiz);
        }

        public async Task<QuizResponseDto> GetQuizByIdAsync(Guid id)
        {
            var quiz = await _quizRepo.GetQuizByIdAsync(id);
            if (quiz == null) return null!;
            return _mapper.Map<QuizResponseDto>(quiz);
        }

        public async Task<PaginationResponseDto<QuizResponseDto>> GetAllQuizzesAsync(PaginationRequestDto pagination)
        {
            var quizzes = await _quizRepo.GetAllQuizzesAsync();
            var query = quizzes.AsQueryable();
            
            query = ApplyFilters(query, ExtractFilterValues(pagination).isDeleted, ExtractFilterValues(pagination).showActive);
            query = ApplySearch(query, pagination.SearchTerm);
            query = ApplySortOrder(query, pagination.SortBy, pagination.GetNormalizedSortDirection());
            
            var dtos = _mapper.Map<List<QuizResponseDto>>(query.ToList());
            return PaginationHelper.CreatePagedResponse(dtos, pagination);
        }

        public async Task<PaginationResponseDto<QuizResponseDto>> GetQuizzesByQuizSetIdAsync(Guid quizSetId, PaginationRequestDto pagination = null!)
        {
            pagination ??= new PaginationRequestDto();
            var quizzes = await _quizRepo.GetQuizzesByQuizSetIdAsync(quizSetId);
            var query = quizzes.AsQueryable();

            query = ApplyFilters(query, ExtractFilterValues(pagination).isDeleted, ExtractFilterValues(pagination).showActive);
            query = ApplySearch(query, pagination.SearchTerm);
            query = ApplySortOrder(query, pagination.SortBy, pagination.GetNormalizedSortDirection());
            
            var dtos = _mapper.Map<List<QuizResponseDto>>(query.ToList());
            return PaginationHelper.CreatePagedResponse(dtos, pagination);
        }

        public async Task<QuizResponseDto> UpdateQuizAsync(Guid id, QuizRequestDto quizDto)
        {
            var updatedQuiz = await _quizRepo.UpdateQuizAsync(id, _mapper.Map<Quiz>(quizDto));
            return _mapper.Map<QuizResponseDto>(updatedQuiz);
        }

        public async Task<bool> SoftDeleteQuizAsync(Guid id)
        {
            return await _quizRepo.SoftDeleteQuizAsync(id);
        }

        public async Task<bool> HardDeleteQuizAsync(Guid id)
        {
            return await _quizRepo.HardDeleteQuizAsync(id);
        }

        public async Task<bool> RestoreQuizAsync(Guid id)
        {
            return await _quizRepo.RestoreQuizAsync(id);
        }

        public async Task<PaginationResponseDto<QuizResponseDto>> GetByGrammarIdAndVocabularyIdAsync(Guid grammarId, Guid vocabularyId, PaginationRequestDto pagination = null!)
        {
            pagination ??= new PaginationRequestDto();
            var quizzes = await _quizRepo.GetByGrammarIdAndVocabularyId(grammarId, vocabularyId);
            var query = quizzes.AsQueryable();
            
            query = ApplySearch(query, pagination.SearchTerm);
            query = ApplySortOrder(query, pagination.SortBy, pagination.GetNormalizedSortDirection());
            
            var dtos = _mapper.Map<List<QuizResponseDto>>(query.ToList());
            return PaginationHelper.CreatePagedResponse(dtos, pagination);
        }

        private static IQueryable<Quiz> ApplySearch(IQueryable<Quiz> query, string? searchTerm)
        {
            if (string.IsNullOrEmpty(searchTerm))
                return query;

            var normalizedSearchTerm = searchTerm.ToLower();
            
            return query.Where(q =>
                (!string.IsNullOrEmpty(q.QuestionText) && q.QuestionText.ToLower().Contains(normalizedSearchTerm)) ||
                (!string.IsNullOrEmpty(q.CorrectAnswer) && q.CorrectAnswer.ToLower().Contains(normalizedSearchTerm)) ||
                (!string.IsNullOrEmpty(q.TOEICPart) && q.TOEICPart.ToLower().Contains(normalizedSearchTerm)));
        }

        private static IQueryable<Quiz> ApplySortOrder(IQueryable<Quiz> query, string? sortBy, string sortDirection)
        {
            var sortField = sortBy ?? "CreatedAt";
            
            return sortField.ToLower() switch
            {
                "createdat" => sortDirection == "desc" 
                    ? query.OrderByDescending(q => q.CreatedAt)
                    : query.OrderBy(q => q.CreatedAt),
                "updatedat" => sortDirection == "desc" 
                    ? query.OrderByDescending(q => q.UpdatedAt)
                    : query.OrderBy(q => q.UpdatedAt),
                "questiontext" => sortDirection == "desc" 
                    ? query.OrderByDescending(q => q.QuestionText)
                    : query.OrderBy(q => q.QuestionText),
                "correctanswer" => sortDirection == "desc" 
                    ? query.OrderByDescending(q => q.CorrectAnswer)
                    : query.OrderBy(q => q.CorrectAnswer),
                "toeicpart" => sortDirection == "desc" 
                    ? query.OrderByDescending(q => q.TOEICPart)
                    : query.OrderBy(q => q.TOEICPart),
                _ => query.OrderByDescending(q => q.CreatedAt)
            };
        }

        private (bool? showActive, bool? isDeleted) ExtractFilterValues(PaginationRequestDto pagination)
        {
            var jsonExtractHelper = new JsonExtractHelper();
            if (pagination.Filters == null)
                return (null, null);

            bool? showActive = jsonExtractHelper.GetBoolFromFilter(pagination.Filters, "isActive");
            bool? showDeleted = jsonExtractHelper.GetBoolFromFilter(pagination.Filters, "isDeleted");

            return (showActive, showDeleted);
        }

        private static IQueryable<Quiz> ApplyFilters(
            IQueryable<Quiz> query,
            bool? isDeleted = null,
            bool? isActive = null)
        {
            if (isDeleted.HasValue)
            {
                if (isDeleted.Value)
                {
                    query = query.Where(q => q.DeletedAt != null);
                }
                else
                {
                    query = query.Where(q => q.DeletedAt == null);
                }
            }
            else
            {
                query = query.Where(q => q.DeletedAt == null);
            }

            
            if (isActive.HasValue)
            {
                query = query.Where(q => q.IsActive == isActive.Value);
            }

            return query;
        }
    }
}
