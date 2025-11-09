using AutoMapper;
using BusinessLogic.DTOs;
using BusinessLogic.DTOs.UserMistakeDtos;
using BusinessLogic.Interfaces;
using Repository.Entities;
using Repository.Interfaces;

namespace BusinessLogic.Services
{
    public class QuizAttemptDetailService : IQuizAttemptDetailService
    {
        private readonly IQuizAttemptDetailRepo _repo;
        private readonly IQuizAttemptRepo _attemptRepo;
        private readonly IAnswerOptionRepo _answerOptionRepo;
        private readonly IUserMistakeService _userMistakeService;
        private readonly IUserMistakeRepo _userMistakeRepo;
        private readonly IMapper _mapper;

        public QuizAttemptDetailService(IQuizAttemptDetailRepo repo, IQuizAttemptRepo attemptRepo, IAnswerOptionRepo answerOptionRepo, IUserMistakeService userMistakeService, IUserMistakeRepo userMistakeRepo, IMapper mapper)
        {
            _repo = repo;
            _attemptRepo = attemptRepo;
            _answerOptionRepo = answerOptionRepo;
            _userMistakeService = userMistakeService;
            _userMistakeRepo = userMistakeRepo;
            _mapper = mapper;
        }

        public async Task<ResponseQuizAttemptDetailDto> CreateAsync(RequestQuizAttemptDetailDto dto)
        {
            var entity = _mapper.Map<QuizAttemptDetail>(dto);
            var created = await _repo.CreateAsync(entity);
            return _mapper.Map<ResponseQuizAttemptDetailDto>(created);
        }

        public async Task<IEnumerable<ResponseQuizAttemptDetailDto>> GetAllAsync(bool includeDeleted = false)
        {
            var list = await _repo.GetAllAsync(includeDeleted);
            return _mapper.Map<IEnumerable<ResponseQuizAttemptDetailDto>>(list);
        }

        public async Task<ResponseQuizAttemptDetailDto?> GetByIdAsync(Guid id)
        {
            var entity = await _repo.GetByIdAsync(id);
            return entity == null ? null : _mapper.Map<ResponseQuizAttemptDetailDto>(entity);
        }

        public async Task<IEnumerable<ResponseQuizAttemptDetailDto>> GetByAttemptIdAsync(Guid attemptId, bool includeDeleted = false)
        {
            var list = await _repo.GetByAttemptIdAsync(attemptId, includeDeleted);
            return _mapper.Map<IEnumerable<ResponseQuizAttemptDetailDto>>(list);
        }

        public async Task<bool> RestoreAsync(Guid id)
        {
            return await _repo.RestoreAsync(id);
        }

        public async Task<bool> SoftDeleteAsync(Guid id)
        {
            return await _repo.SoftDeleteAsync(id);
        }

        public async Task<ResponseQuizAttemptDetailDto?> UpdateAsync(Guid id, RequestQuizAttemptDetailDto dto)
        {
            var entity = _mapper.Map<QuizAttemptDetail>(dto);
            var updated = await _repo.UpdateAsync(id, entity);
            return updated == null ? null : _mapper.Map<ResponseQuizAttemptDetailDto>(updated);
        }

        public async Task<ResponseSubmitAnswersDto> SubmitAnswersAsync(RequestSubmitAnswersDto dto)
        {
            var attempt = await _attemptRepo.GetByIdAsync(dto.AttemptId);
            if (attempt == null)
            {
                throw new InvalidOperationException("Attempt not found");
            }

            int correctCount = 0;
            int wrongCount = 0;
            int totalTimeSpent = 0;
            var answerResults = new List<AnswerResultDto>();

            // Lưu và chấm điểm từng câu trả lời
            foreach (var answer in dto.Answers)
            {
                // Kiểm tra đáp án đúng
                bool isCorrect = false;
                Guid? correctAnswerOptionId = null;

                if (Guid.TryParse(answer.UserAnswer, out Guid selectedAnswerOptionId))
                {
                    var selectedAnswerOption = await _answerOptionRepo.GetByIdAsync(selectedAnswerOptionId);
                    
                    if (selectedAnswerOption != null && selectedAnswerOption.QuizId == answer.QuestionId)
                    {
                        isCorrect = selectedAnswerOption.IsCorrect;
                        
                        // Tìm đáp án đúng (nếu người dùng chọn sai)
                        if (!isCorrect)
                        {
                            var answerOptions = await _answerOptionRepo.GetByQuizIdAsync(answer.QuestionId);
                            var correctOption = answerOptions.FirstOrDefault(ao => ao.IsCorrect);
                            correctAnswerOptionId = correctOption?.Id;
                        }
                    }
                }
                else
                {
                    // Nếu không parse được, tìm đáp án đúng
                    var answerOptions = await _answerOptionRepo.GetByQuizIdAsync(answer.QuestionId);
                    var correctOption = answerOptions.FirstOrDefault(ao => ao.IsCorrect);
                    correctAnswerOptionId = correctOption?.Id;
                }

                // Tạo QuizAttemptDetail
                var detail = new QuizAttemptDetail
                {
                    AttemptId = dto.AttemptId,
                    QuestionId = answer.QuestionId,
                    UserAnswer = answer.UserAnswer,
                    IsCorrect = isCorrect,
                    TimeSpent = answer.TimeSpent,
                    QuizId = answer.QuestionId,
                    QuizAttemptId = dto.AttemptId
                };

                await _repo.CreateAsync(detail);

                // Tính tổng thời gian
                if (answer.TimeSpent.HasValue)
                {
                    totalTimeSpent += answer.TimeSpent.Value;
                }

                // Đếm số câu đúng/sai
                if (isCorrect)
                {
                    correctCount++;
                }
                else
                {
                    wrongCount++;
                }

                // Thêm vào kết quả
                answerResults.Add(new AnswerResultDto
                {
                    QuestionId = answer.QuestionId,
                    IsCorrect = isCorrect,
                    CorrectAnswerOptionId = correctAnswerOptionId
                });
            }

            // Cập nhật QuizAttempt với kết quả
            attempt.CorrectAnswers = correctCount;
            attempt.WrongAnswers = wrongCount;
            attempt.Score = correctCount;
            attempt.Accuracy = attempt.TotalQuestions > 0 ? (decimal)correctCount / attempt.TotalQuestions : 0;
            attempt.Status = "completed";
            attempt.TimeSpent = totalTimeSpent > 0 ? totalTimeSpent : null;

            await _attemptRepo.UpdateAsync(dto.AttemptId, attempt);

            var response = new ResponseSubmitAnswersDto
            {
                AttemptId = dto.AttemptId,
                TotalQuestions = attempt.TotalQuestions,
                CorrectAnswers = correctCount,
                WrongAnswers = wrongCount,
                Score = correctCount,
                Accuracy = attempt.Accuracy,
                Status = attempt.Status,
                AnswerResults = answerResults
            };

            // Chạy ngầm: Tạo/update UserMistake cho các câu trả lời sai
            _ = Task.Run(async () =>
            {
                try
                {
                    // Lấy danh sách QuizId (QuestionId) từ các câu trả lời sai
                    var wrongQuestionIds = answerResults
                        .Where(ar => !ar.IsCorrect)
                        .Select(ar => ar.QuestionId)
                        .Distinct()
                        .ToList();

                    if (wrongQuestionIds.Any() && attempt.UserId != Guid.Empty)
                    {
                        // Tạo/update UserMistake cho mỗi câu hỏi sai
                        foreach (var quizId in wrongQuestionIds)
                        {
                            var existingMistake = await _userMistakeRepo.GetByUserIdAndQuizIdAsync(attempt.UserId, quizId);
                            
                            if (existingMistake == null)
                            {
                                // Tạo mới bằng AddAsync
                                await _userMistakeService.AddAsync(new RequestUserMistakeDto
                                {
                                    UserId = attempt.UserId,
                                    QuizId = quizId,
                                    TimesAttempted = 1,
                                    TimesWrong = 1,
                                    LastAttemptedAt = DateTime.UtcNow,
                                    IsAnalyzed = false
                                });
                            }
                            else
                            {
                                // Update bằng UpdateAsync
                                await _userMistakeService.UpdateAsync(existingMistake.Id, new RequestUserMistakeDto
                                {
                                    UserId = attempt.UserId,
                                    QuizId = quizId,
                                    TimesAttempted = existingMistake.TimesAttempted + 1,
                                    TimesWrong = existingMistake.TimesWrong + 1,
                                    LastAttemptedAt = DateTime.UtcNow,
                                    IsAnalyzed = existingMistake.IsAnalyzed
                                });
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Log lỗi nhưng không ảnh hưởng đến response
                    // Có thể thêm logger ở đây nếu cần
                }
            });

            return response;
        }
    }
}
