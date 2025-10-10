using AutoMapper;
using BusinessLogic.DTOs;
using BusinessLogic.Interfaces;
using Repository.Entities;
using Repository.Interfaces;

namespace BusinessLogic.Services
{
    public class QuizAttemptService : IQuizAttemptService
    {
        private readonly IQuizAttemptRepo _repo;
        private readonly IQuizAttemptDetailRepo _detailRepo;
        private readonly IQuizRepo _quizRepo;
        private readonly IMapper _mapper;

        public QuizAttemptService(IQuizAttemptRepo repo, IQuizAttemptDetailRepo detailRepo, IQuizRepo quizRepo, IMapper mapper)
        {
            _repo = repo;
            _detailRepo = detailRepo;
            _quizRepo = quizRepo;
            _mapper = mapper;
        }

        public async Task<ResponseQuizAttemptDto> CreateAsync(RequestQuizAttemptDto dto)
        {
            var entity = _mapper.Map<QuizAttempt>(dto);
            var created = await _repo.CreateAsync(entity);
            return _mapper.Map<ResponseQuizAttemptDto>(created);
        }

        public async Task<ResponseSingleStartDto> StartSingleAsync(RequestSingleStartDto dto)
        {
            // Fetch ALL questions for the quiz set (no subset selection)
            var allQuestions = await _quizRepo.GetQuizzesByQuizSetIdAsync(dto.QuizSetId);
            var selected = allQuestions
                .OrderBy(q => q.OrderIndex ?? int.MaxValue)
                .ThenBy(q => q.CreatedAt)
                .ToList();

            // Create attempt in progress
            var attempt = new QuizAttempt
            {
                UserId = dto.UserId,
                QuizSetId = dto.QuizSetId,
                AttemptType = "single",
                TotalQuestions = selected.Count,
                CorrectAnswers = 0,
                WrongAnswers = 0,
                Score = 0,
                Accuracy = 0,
                TimeSpent = null,
                OpponentId = null,
                IsWinner = null,
                Status = "in_progress"
            };

            var created = await _repo.CreateAsync(attempt);

            // Map to QuizResponseDto using mapper
            var quizDtos = _mapper.Map<IEnumerable<BusinessLogic.DTOs.QuizDtos.QuizResponseDto>>(selected)
                                  .Select(q => { q.CorrectAnswer = string.Empty; return q; })
                                  .ToList();

            return new ResponseSingleStartDto
            {
                AttemptId = created.Id,
                TotalQuestions = created.TotalQuestions,
                Questions = quizDtos
            };
        }

        public async Task<IEnumerable<ResponseQuizAttemptDto>> GetAllAsync(bool includeDeleted = false)
        {
            var list = await _repo.GetAllAsync(includeDeleted);
            return _mapper.Map<IEnumerable<ResponseQuizAttemptDto>>(list);
        }

        public async Task<ResponseQuizAttemptDto?> GetByIdAsync(Guid id)
        {
            var entity = await _repo.GetByIdAsync(id);
            return entity == null ? null : _mapper.Map<ResponseQuizAttemptDto>(entity);
        }

        public async Task<IEnumerable<ResponseQuizAttemptDto>> GetByUserIdAsync(Guid userId, bool includeDeleted = false)
        {
            var list = await _repo.GetByUserIdAsync(userId, includeDeleted);
            return _mapper.Map<IEnumerable<ResponseQuizAttemptDto>>(list);
        }

        public async Task<IEnumerable<ResponseQuizAttemptDto>> GetByQuizSetIdAsync(Guid quizSetId, bool includeDeleted = false)
        {
            var list = await _repo.GetByQuizSetIdAsync(quizSetId, includeDeleted);
            return _mapper.Map<IEnumerable<ResponseQuizAttemptDto>>(list);
        }

        public async Task<bool> RestoreAsync(Guid id)
        {
            return await _repo.RestoreAsync(id);
        }

        public async Task<bool> SoftDeleteAsync(Guid id)
        {
            return await _repo.SoftDeleteAsync(id);
        }

        public async Task<ResponseQuizAttemptDto?> UpdateAsync(Guid id, RequestQuizAttemptDto dto)
        {
            var entity = _mapper.Map<QuizAttempt>(dto);
            var updated = await _repo.UpdateAsync(id, entity);
            return updated == null ? null : _mapper.Map<ResponseQuizAttemptDto>(updated);
        }

        public async Task<ResponseQuizAttemptDto?> FinishAsync(Guid id)
        {
            var attempt = await _repo.GetByIdAsync(id);
            if (attempt == null) return null;

            var details = await _detailRepo.GetByAttemptIdAsync(id, includeDeleted: false);
            var detailList = details.ToList();
            int correct = 0;
            int wrong = 0;
            int score = 0;
            foreach (var d in detailList)
            {
                var quiz = await _quizRepo.GetQuizByIdAsync(d.QuestionId);
                bool isCorrect = string.Equals(d.UserAnswer, quiz.CorrectAnswer, StringComparison.OrdinalIgnoreCase);
                if (isCorrect) { correct++; score++; }
                else { wrong++; }

                if (d.IsCorrect != isCorrect)
                {
                    d.IsCorrect = isCorrect;
                    await _detailRepo.UpdateAsync(d.Id, d);
                }
            }

            attempt.CorrectAnswers = correct;
            attempt.WrongAnswers = wrong;
            attempt.Score = score;
            attempt.Accuracy = attempt.TotalQuestions > 0 ? (decimal)correct / attempt.TotalQuestions : 0;
            attempt.Status = "completed";

            var updatedAttempt = await _repo.UpdateAsync(id, attempt);
            return updatedAttempt == null ? null : _mapper.Map<ResponseQuizAttemptDto>(updatedAttempt);
        }

        public async Task<PlayerHistoryResponseDto> GetPlayerHistoryAsync(PlayerHistoryRequestDto request)
        {
            var allAttempts = await _repo.GetByUserIdAsync(request.UserId, includeDeleted: false);
            var attempts = allAttempts.AsQueryable();

            // Apply filters
            if (request.QuizSetId.HasValue)
                attempts = attempts.Where(a => a.QuizSetId == request.QuizSetId.Value);

            if (!string.IsNullOrEmpty(request.Status))
                attempts = attempts.Where(a => a.Status == request.Status);

            if (!string.IsNullOrEmpty(request.AttemptType))
                attempts = attempts.Where(a => a.AttemptType == request.AttemptType);

            // Apply sorting
            attempts = request.SortBy.ToLower() switch
            {
                "score" => request.SortOrder.ToLower() == "asc" ? attempts.OrderBy(a => a.Score) : attempts.OrderByDescending(a => a.Score),
                "accuracy" => request.SortOrder.ToLower() == "asc" ? attempts.OrderBy(a => a.Accuracy) : attempts.OrderByDescending(a => a.Accuracy),
                _ => request.SortOrder.ToLower() == "asc" ? attempts.OrderBy(a => a.CreatedAt) : attempts.OrderByDescending(a => a.CreatedAt)
            };

            var totalCount = attempts.Count();
            var pagedAttempts = attempts
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToList();

            return new PlayerHistoryResponseDto
            {
                Attempts = _mapper.Map<IEnumerable<ResponseQuizAttemptDto>>(pagedAttempts),
                TotalCount = totalCount,
                Page = request.Page,
                PageSize = request.PageSize
            };
        }

        public async Task<PlayerStatsDto> GetPlayerStatsAsync(Guid userId)
        {
            var allAttempts = await _repo.GetByUserIdAsync(userId, includeDeleted: false);
            var attempts = allAttempts.ToList();

            var completedAttempts = attempts.Where(a => a.Status == "completed").ToList();
            var inProgressAttempts = attempts.Where(a => a.Status == "in_progress").ToList();

            var totalQuestions = completedAttempts.Sum(a => a.TotalQuestions);
            var totalCorrect = completedAttempts.Sum(a => a.CorrectAnswers);
            var totalTimeSpent = completedAttempts.Where(a => a.TimeSpent.HasValue).Sum(a => a.TimeSpent.Value);

            return new PlayerStatsDto
            {
                UserId = userId,
                TotalAttempts = attempts.Count,
                CompletedAttempts = completedAttempts.Count,
                InProgressAttempts = inProgressAttempts.Count,
                AverageScore = completedAttempts.Any() ? (decimal)completedAttempts.Average(a => a.Score) : 0,
                AverageAccuracy = completedAttempts.Any() ? completedAttempts.Average(a => a.Accuracy) : 0,
                BestScore = completedAttempts.Any() ? completedAttempts.Max(a => a.Score) : 0,
                BestAccuracy = completedAttempts.Any() ? completedAttempts.Max(a => a.Accuracy) : 0,
                TotalQuestionsAnswered = totalQuestions,
                TotalCorrectAnswers = totalCorrect,
                TotalTimeSpent = totalTimeSpent > 0 ? TimeSpan.FromSeconds(totalTimeSpent) : null,
                LastPlayedAt = attempts.Any() ? attempts.Max(a => a.CreatedAt) : null
            };
        }
    }
}
