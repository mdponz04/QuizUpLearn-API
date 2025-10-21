using AutoMapper;
using BusinessLogic.DTOs.QuizDtos;
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
            if(quizDto.QuizSetId == null || quizDto.QuizSetId == Guid.Empty)
                throw new ArgumentException("QuizSetId cannot be null");

            var quiz = _mapper.Map<Quiz>(quizDto);
            var createdQuiz = await _quizRepo.CreateQuizAsync(quiz);
            return _mapper.Map<QuizResponseDto>(createdQuiz);
        }

        public async Task<QuizResponseDto> GetQuizByIdAsync(Guid id)
        {
            var quiz = await _quizRepo.GetQuizByIdAsync(id);
            return _mapper.Map<QuizResponseDto>(quiz);
        }

        public async Task<IEnumerable<QuizResponseDto>> GetAllQuizzesAsync()
        {
            var quizzes = await _quizRepo.GetAllQuizzesAsync();
            return _mapper.Map<IEnumerable<QuizResponseDto>>(quizzes);
        }

        public async Task<IEnumerable<QuizResponseDto>> GetQuizzesByQuizSetIdAsync(Guid quizSetId)
        {
            var quizzes = await _quizRepo.GetQuizzesByQuizSetIdAsync(quizSetId);
            return _mapper.Map<IEnumerable<QuizResponseDto>>(quizzes);
        }

        public async Task<IEnumerable<QuizResponseDto>> GetActiveQuizzesAsync()
        {
            var quizzes = await _quizRepo.GetActiveQuizzesAsync();
            return _mapper.Map<IEnumerable<QuizResponseDto>>(quizzes);
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
    }
}
