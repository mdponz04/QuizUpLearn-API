using AutoMapper;
using BusinessLogic.DTOs.QuizSetDtos;
using BusinessLogic.Interfaces;
using Repository.Entities;
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

        public async Task<IEnumerable<QuizSetResponseDto>> GetAllQuizSetsAsync(bool includeDeleted)
        {
            var quizSets = await _quizSetRepo.GetAllQuizSetsAsync(includeDeleted);
            return _mapper.Map<IEnumerable<QuizSetResponseDto>>(quizSets);
        }

        public async Task<IEnumerable<QuizSetResponseDto>> GetQuizSetsByCreatorAsync(Guid creatorId)
        {
            var quizSets = await _quizSetRepo.GetQuizSetsByCreatorAsync(creatorId);
            return _mapper.Map<IEnumerable<QuizSetResponseDto>>(quizSets);
        }

        public async Task<IEnumerable<QuizSetResponseDto>> GetPublishedQuizSetsAsync()
        {
            var quizSets = await _quizSetRepo.GetPublishedQuizSetsAsync();
            return _mapper.Map<IEnumerable<QuizSetResponseDto>>(quizSets);
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
    }
}
