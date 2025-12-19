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

            var quizSets = await _quizSetRepo.GetAllQuizSetsAsync( 
                pagination.SearchTerm, 
                pagination.SortBy, 
                pagination.SortDirection,
                filters.isDeleted,
                filters.isPremiumOnly,
                filters.isPublished,
                filters.quizSetType);

            var dtos = _mapper.Map<IEnumerable<QuizSetResponseDto>>(quizSets);
            return PaginationHelper.CreatePagedResponse(dtos, pagination);
        }

        public async Task<PaginationResponseDto<QuizSetResponseDto>> GetQuizSetsByCreatorAsync(Guid creatorId, PaginationRequestDto pagination)
        {
            var filters = ExtractFilterValues(pagination);

            var quizSets = await _quizSetRepo.GetQuizSetsByCreatorAsync(
                creatorId, 
                pagination.SearchTerm, 
                pagination.SortBy, 
                pagination.SortDirection,
                filters.isDeleted,
                filters.isPremiumOnly,
                filters.isPublished,
                filters.quizSetType);

            var dtos = _mapper.Map<IEnumerable<QuizSetResponseDto>>(quizSets);
            return PaginationHelper.CreatePagedResponse(dtos, pagination);
        }

        public async Task<PaginationResponseDto<QuizSetResponseDto>> GetPublishedQuizSetsAsync(PaginationRequestDto pagination)
        {
            var filters = ExtractFilterValues(pagination);

            var quizSets = await _quizSetRepo.GetPublishedQuizSetsAsync(
                pagination.SearchTerm,
                pagination.SortBy,
                pagination.SortDirection,
                filters.isPremiumOnly,
                filters.quizSetType);

            var dtos = _mapper.Map<IEnumerable<QuizSetResponseDto>>(quizSets);
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
            return await _quizSetRepo.RequestValidateByMod(id);
        }

        public async Task<bool> ValidateQuizSetAsync(Guid id)
        {
            return await _quizSetRepo.ValidateQuizSet(id);
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
