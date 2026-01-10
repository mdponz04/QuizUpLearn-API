using AutoMapper;
using BusinessLogic.DTOs;
using BusinessLogic.DTOs.QuizDtos;
using BusinessLogic.Helpers;
using BusinessLogic.Interfaces;
using Repository.Entities;
using Repository.Enums;
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
            if(quizDto.QuestionText == null || quizDto.TOEICPart == null)
            {
                throw new ArgumentException("QuestionText and TOEICPart cannot be null");
            }
            var quiz = _mapper.Map<Quiz>(quizDto);
            var createdQuiz = await _quizRepo.CreateQuizAsync(quiz);
            return _mapper.Map<QuizResponseDto>(createdQuiz);
        }

        public async Task<QuizResponseDto> GetQuizByIdAsync(Guid id)
        {
            if(id == Guid.Empty)
            {
                throw new ArgumentNullException("Quiz ID cannot be empty");
            }
            var quiz = await _quizRepo.GetQuizByIdAsync(id);
            if (quiz == null) return null!;
            return _mapper.Map<QuizResponseDto>(quiz);
        }

        public async Task<PaginationResponseDto<QuizResponseDto>> GetAllQuizzesAsync(PaginationRequestDto pagination)
        {
            var quizzes = await _quizRepo.GetAllQuizzesAsync();
            var query = quizzes.AsQueryable();

            var filters = ExtractFilterValues(pagination);
            query = ApplyFilters(query, filters.isDeleted, filters.showActive, filters.isAiGenerated);
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

            var filters = ExtractFilterValues(pagination);
            query = ApplyFilters(query, filters.isDeleted, filters.showActive, filters.isAiGenerated);
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

        public async Task<IEnumerable<QuizResponseDto>> GetByGrammarIdAndVocabularyIdAsync(Guid grammarId, Guid vocabularyId)
        {
            var quizzes = await _quizRepo.GetByGrammarIdAndVocabularyId(grammarId, vocabularyId);
            return _mapper.Map<List<QuizResponseDto>>(quizzes.ToList());
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

        private (bool? showActive, bool? isDeleted, bool? isAiGenerated) ExtractFilterValues(PaginationRequestDto pagination)
        {
            var jsonExtractHelper = new JsonExtractHelper();
            if (pagination.Filters == null)
                return (null, null, null);

            bool? showActive = jsonExtractHelper.GetBoolFromFilter(pagination.Filters, "isActive");
            bool? showDeleted = jsonExtractHelper.GetBoolFromFilter(pagination.Filters, "isDeleted");
            bool? showAiGenerated = jsonExtractHelper.GetBoolFromFilter(pagination.Filters, "isAiGenerated");

            return (showActive, showDeleted, showAiGenerated);
        }

        private static IQueryable<Quiz> ApplyFilters(
            IQueryable<Quiz> query,
            bool? isDeleted = null,
            bool? isActive = null,
            bool? isAiGenerated = null)
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

            if(isAiGenerated.HasValue)
            {
                query = query.Where(q => q.IsAIGenerated == isAiGenerated.Value);
            }
            else
            {
                query = query.Where(q => q.IsAIGenerated == true);
            }

            return query;
        }

        static readonly int[,] ToeicMatrix = {
            //Parts
            {  4, 10, 10,  6, 10,  4,  8 }, // Easy
            {  2, 10, 18, 14, 12,  8, 20 }, // Medium
            {  0,  5, 11, 10,  8,  4, 26 } // Hard
        };

        public async Task<NeedAmountQuizResponseDto> GetNeededQuizCountsForTOEICAsync(List<Guid> quizIds)
        {
            var quizzes =  new List<Quiz>();
            foreach(var id in quizIds)
            {
                var quiz = await _quizRepo.GetQuizByIdAsync(id);
                if (quiz != null)
                {
                    quizzes.Add(quiz);
                }
            }

            int[,] needed = new int[3, 7];

            for (int part = 0; part < 7; part++)
            {
                string partString = "";
                switch (part)
                {
                    case 0:
                        partString = QuizPartEnum.PART1.ToString();
                        break;
                    case 1:
                        partString = QuizPartEnum.PART2.ToString();
                        break;
                    case 2:
                        partString = QuizPartEnum.PART3.ToString();
                        break;
                    case 3:
                        partString = QuizPartEnum.PART4.ToString();
                        break;
                    case 4:
                        partString = QuizPartEnum.PART5.ToString();
                        break;
                    case 5:
                        partString = QuizPartEnum.PART6.ToString();
                        break;
                    case 6:
                        partString = QuizPartEnum.PART7.ToString();
                        break;
                }

                var quizzesByPart = quizzes.Where(q => q.TOEICPart == partString).ToList();

                int currentEasy = quizzesByPart.Count(q => q.DifficultyLevel.ToLower() == "easy");
                int currentMedium = quizzesByPart.Count(q => q.DifficultyLevel.ToLower() == "medium");
                int currentHard = quizzesByPart.Count(q => q.DifficultyLevel.ToLower() == "hard");

                needed[0, part] = ToeicMatrix[0, part] - currentEasy;
                needed[1, part] = ToeicMatrix[1, part] - currentMedium;
                needed[2, part] = ToeicMatrix[2, part] - currentHard;
            }

            return new NeedAmountQuizResponseDto{
                Part1EasyAmount = needed[0,0],
                Part1MediumAmount = needed[1,0],
                Part1HardAmount = needed[2,0],
                Part2EasyAmount = needed[0,1],
                Part2MediumAmount = needed[1,1],
                Part2HardAmount = needed[2,1],
                Part3EasyAmount = needed[0,2],
                Part3MediumAmount = needed[1,2],
                Part3HardAmount = needed[2,2],
                Part4EasyAmount = needed[0,3],
                Part4MediumAmount = needed[1,3],
                Part4HardAmount = needed[2,3],
                Part5EasyAmount = needed[0,4],
                Part5MediumAmount = needed[1,4],
                Part5HardAmount = needed[2,4],
                Part6EasyAmount = needed[0,5],
                Part6MediumAmount = needed[1,5],
                Part6HardAmount = needed[2,5],
                Part7EasyAmount = needed[0,6],
                Part7MediumAmount = needed[1,6],
                Part7HardAmount = needed[2,6],
            };
        }
    }
}
