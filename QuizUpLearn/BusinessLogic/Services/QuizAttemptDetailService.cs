using AutoMapper;
using BusinessLogic.DTOs;
using BusinessLogic.DTOs.UserMistakeDtos;
using BusinessLogic.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Repository.Entities;
using Repository.Interfaces;

namespace BusinessLogic.Services
{
    public class QuizAttemptDetailService : IQuizAttemptDetailService
    {
        private readonly IQuizAttemptDetailRepo _repo;
        private readonly IQuizAttemptRepo _attemptRepo;
        private readonly IAnswerOptionRepo _answerOptionRepo;
        private readonly IQuizRepo _quizRepo;
            private readonly IUserRepo _userRepo;
        private readonly IUserMistakeService _userMistakeService;
        private readonly IUserMistakeRepo _userMistakeRepo;
        private readonly IAIService _aiService;
        private readonly IMapper _mapper;
        private readonly IServiceScopeFactory _scopeFactory;

        public QuizAttemptDetailService(
            IQuizAttemptDetailRepo repo,
            IQuizAttemptRepo attemptRepo,
            IAnswerOptionRepo answerOptionRepo,
            IQuizRepo quizRepo,
                IUserRepo userRepo,
            IUserMistakeService userMistakeService,
            IUserMistakeRepo userMistakeRepo,
            IAIService aiService,
            IMapper mapper,
            IServiceScopeFactory scopeFactory)
        {
            _repo = repo;
            _attemptRepo = attemptRepo;
            _answerOptionRepo = answerOptionRepo;
            _quizRepo = quizRepo;
            _userRepo = userRepo;
            _userMistakeService = userMistakeService;
            _userMistakeRepo = userMistakeRepo;
            _aiService = aiService;
            _mapper = mapper;
            _scopeFactory = scopeFactory;
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

        public async Task<ResponsePlacementTestDto> GetPlacementTestByAttemptIdAsync(Guid attemptId)
        {
            var attempt = await _attemptRepo.GetByIdAsync(attemptId);
            if (attempt == null)
            {
                throw new InvalidOperationException("Attempt not found");
            }

            if (attempt.AttemptType != "placement")
            {
                throw new InvalidOperationException("This attempt is not a placement test");
            }

            // Lấy QuizAttemptDetails để tính LisPoint và ReaPoint
            var details = await _repo.GetByAttemptIdAsync(attemptId, includeDeleted: false);
            var detailList = details.ToList();

            int correctLisCount = 0;
            int correctReaCount = 0;

            foreach (var detail in detailList)
            {
                if (detail.IsCorrect == true)
                {
                    // Lấy Quiz để biết TOEICPart
                    var quiz = await _quizRepo.GetQuizByIdAsync(detail.QuestionId);
                    if (quiz != null)
                    {
                        var isListening = quiz.TOEICPart == "PART1" || quiz.TOEICPart == "PART2" ||
                                        quiz.TOEICPart == "PART3" || quiz.TOEICPart == "PART4";
                        
                        if (isListening)
                            correctLisCount++;
                        else
                            correctReaCount++;
                    }
                }
            }

            // Tính LisPoint và ReaPoint
            int lisPoint = ConvertToTOEICScore(correctLisCount, isListening: true);
            int reaPoint = ConvertToTOEICScore(correctReaCount, isListening: false);

            return new ResponsePlacementTestDto
            {
                AttemptId = attemptId,
                LisPoint = lisPoint,
                TotalCorrectLisAns = correctLisCount,
                ReaPoint = reaPoint,
                TotalCorrectReaAns = correctReaCount,
                TotalQuestions = attempt.TotalQuestions,
                Status = attempt.Status
            };
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
            var wrongQuestionIdsSet = new HashSet<Guid>();

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
                    wrongQuestionIdsSet.Add(answer.QuestionId);
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
            attempt.TimeSpent = totalTimeSpent > 0 ? totalTimeSpent : (int?)null;

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

            // Nền 1: Tạo/update UserMistake cho các câu trả lời sai
            var wrongQuestionIds = wrongQuestionIdsSet.ToList();
            var userId = attempt.UserId;
            var attemptId = dto.AttemptId;

            var userMistakeTask = Task.Run(async () =>
            {
                if (!wrongQuestionIds.Any() || userId == Guid.Empty)
                {
                    return;
                }

                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var userMistakeRepo = scope.ServiceProvider.GetRequiredService<IUserMistakeRepo>();
                    var userMistakeService = scope.ServiceProvider.GetRequiredService<IUserMistakeService>();

                    foreach (var quizId in wrongQuestionIds)
                    {
                        try
                        {
                            var existingMistake = await userMistakeRepo.GetByUserIdAndQuizIdAsync(userId, quizId);
                            
                            if (existingMistake == null)
                            {
                                await userMistakeService.AddAsync(new RequestUserMistakeDto
                                {
                                    UserId = userId,
                                    QuizId = quizId,
                                    TimesAttempted = 1,
                                    TimesWrong = 1,
                                    LastAttemptedAt = DateTime.UtcNow,
                                    IsAnalyzed = false
                                });
                            }
                            else
                            {
                                await userMistakeService.UpdateAsync(existingMistake.Id, new RequestUserMistakeDto
                                {
                                    UserId = userId,
                                    QuizId = quizId,
                                    TimesAttempted = existingMistake.TimesAttempted + 1,
                                    TimesWrong = existingMistake.TimesWrong + 1,
                                    LastAttemptedAt = DateTime.UtcNow,
                                    IsAnalyzed = existingMistake.IsAnalyzed
                                });
                            }
                        }
                        catch (Exception ex)
                        {
                            // Log error for individual quiz
                            // Could add logging here if needed
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Log error for background UserMistake processing
                    // Could add logging here if needed
                }
            });

            // Nền 2: Sau khi cập nhật UserMistake xong thì chạy phân tích AI (không ảnh hưởng response)
            _ = Task.Run(async () =>
            {
                try
                {
                    await userMistakeTask; // đảm bảo dữ liệu sai đã được lưu
                    if (userId != Guid.Empty)
                    {
                        using var scope = _scopeFactory.CreateScope();
                        var aiService = scope.ServiceProvider.GetRequiredService<IAIService>();
                        await aiService.AnalyzeUserMistakesAndAdviseAsync(userId);
                    }
                }
                catch (Exception ex)
                {
                    // Log error for background AI analysis
                    // Could add logging here if needed
                }
            });

            // Trả về ngay, không chờ AI phân tích
            return response;
        }

        public async Task<ResponsePlacementTestDto> SubmitPlacementTestAsync(RequestSubmitAnswersDto dto)
        {
            var attempt = await _attemptRepo.GetByIdAsync(dto.AttemptId);
            if (attempt == null)
            {
                throw new InvalidOperationException("Attempt not found");
            }

            int totalTimeSpent = 0;
            int correctLisCount = 0;
            int correctReaCount = 0;
            var wrongQuestionIdsSet = new HashSet<Guid>();
            var wrongAnswersByQuestion = new Dictionary<Guid, string>();

            // Lưu và chấm điểm từng câu trả lời
            foreach (var answer in dto.Answers)
            {
                // Lấy thông tin Quiz để biết TOEICPart
                var quiz = await _quizRepo.GetQuizByIdAsync(answer.QuestionId);
                if (quiz == null)
                {
                    continue;
                }

                // Kiểm tra đáp án đúng
                bool isCorrect = false;
                if (Guid.TryParse(answer.UserAnswer, out Guid selectedAnswerOptionId))
                {
                    var selectedAnswerOption = await _answerOptionRepo.GetByIdAsync(selectedAnswerOptionId);
                    if (selectedAnswerOption != null && selectedAnswerOption.QuizId == answer.QuestionId)
                    {
                        isCorrect = selectedAnswerOption.IsCorrect;
                    }
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

                if (answer.TimeSpent.HasValue)
                {
                    totalTimeSpent += answer.TimeSpent.Value;
                }

                // Phân loại Listening/Reading và đếm câu đúng
                var isListening = quiz.TOEICPart == "PART1" || quiz.TOEICPart == "PART2" || 
                                  quiz.TOEICPart == "PART3" || quiz.TOEICPart == "PART4";
                
                if (isCorrect)
                {
                    if (isListening)
                        correctLisCount++;
                    else
                        correctReaCount++;
                }
                else
                {
                    wrongQuestionIdsSet.Add(answer.QuestionId);
                    wrongAnswersByQuestion[answer.QuestionId] = answer.UserAnswer;
                }
            }

            // Quy đổi điểm TOEIC
            int lisPoint = ConvertToTOEICScore(correctLisCount, isListening: true);
            int reaPoint = ConvertToTOEICScore(correctReaCount, isListening: false);
            int totalPlacementScore = lisPoint + reaPoint;

            // Cập nhật QuizAttempt để lưu vào history
            attempt.AttemptType = "placement";
            attempt.CorrectAnswers = correctLisCount + correctReaCount;
            attempt.WrongAnswers = attempt.TotalQuestions - attempt.CorrectAnswers;
            attempt.Score = totalPlacementScore;
            attempt.Accuracy = attempt.TotalQuestions > 0 ? (decimal)attempt.CorrectAnswers / attempt.TotalQuestions : 0;
            attempt.Status = "completed";
            attempt.IsCompleted = true;
            attempt.TimeSpent = totalTimeSpent > 0 ? totalTimeSpent : (int?)null;

            await _attemptRepo.UpdateAsync(dto.AttemptId, attempt);

            // Cập nhật TotalPoints của user nếu điểm placement lần này cao hơn
            if (attempt.UserId != Guid.Empty)
            {
                var user = await _userRepo.GetByIdAsync(attempt.UserId);
                if (user != null && totalPlacementScore > user.TotalPoints)
                {
                    user.TotalPoints = totalPlacementScore;
                    await _userRepo.UpdateAsync(user.Id, user);
                }
            }

            var response = new ResponsePlacementTestDto
            {
                AttemptId = dto.AttemptId,
                LisPoint = lisPoint,
                TotalCorrectLisAns = correctLisCount,
                ReaPoint = reaPoint,
                TotalCorrectReaAns = correctReaCount,
                TotalQuestions = attempt.TotalQuestions,
                Status = attempt.Status
            };

            // Nền 1: Tạo/update UserMistake cho các câu trả lời sai
            var wrongQuestionIds = wrongQuestionIdsSet.ToList();
            var wrongAnswersSnapshot = wrongAnswersByQuestion.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            var userId = attempt.UserId;
            var attemptId = dto.AttemptId;

            var userMistakeTask = Task.Run(async () =>
            {
                if (!wrongQuestionIds.Any() || userId == Guid.Empty)
                {
                    return;
                }

                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var userMistakeRepo = scope.ServiceProvider.GetRequiredService<IUserMistakeRepo>();
                    var userMistakeService = scope.ServiceProvider.GetRequiredService<IUserMistakeService>();

                    foreach (var quizId in wrongQuestionIds)
                    {
                        try
                        {
                            var existingMistake = await userMistakeRepo.GetByUserIdAndQuizIdAsync(userId, quizId);
                            wrongAnswersSnapshot.TryGetValue(quizId, out var userAnswer);

                            if (existingMistake == null)
                            {
                                await userMistakeService.AddAsync(new RequestUserMistakeDto
                                {
                                    UserId = userId,
                                    QuizId = quizId,
                                    TimesAttempted = 1,
                                    TimesWrong = 1,
                                    LastAttemptedAt = DateTime.UtcNow,
                                    IsAnalyzed = false,
                                    UserAnswer = userAnswer
                                });
                            }
                            else
                            {
                                await userMistakeService.UpdateAsync(existingMistake.Id, new RequestUserMistakeDto
                                {
                                    UserId = userId,
                                    QuizId = quizId,
                                    TimesAttempted = existingMistake.TimesAttempted + 1,
                                    TimesWrong = existingMistake.TimesWrong + 1,
                                    LastAttemptedAt = DateTime.UtcNow,
                                    IsAnalyzed = existingMistake.IsAnalyzed,
                                    UserAnswer = userAnswer ?? existingMistake.UserAnswer
                                });
                            }
                        }
                        catch (Exception ex)
                        {
                            // Log error for individual quiz
                            // Could add logging here if needed
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Log error for background UserMistake processing
                    // Could add logging here if needed
                }
            });

            // Nền 2: Sau khi cập nhật UserMistake xong thì chạy phân tích AI (không ảnh hưởng response)
            _ = Task.Run(async () =>
            {
                try
                {
                    await userMistakeTask;
                    if (userId != Guid.Empty)
                    {
                        using var scope = _scopeFactory.CreateScope();
                        var aiService = scope.ServiceProvider.GetRequiredService<IAIService>();
                        await aiService.AnalyzeUserMistakesAndAdviseAsync(userId);
                    }
                }
                catch (Exception ex)
                {
                    // Log error for background AI analysis
                    // Could add logging here if needed
                }
            });

            return response;
        }

        private static readonly Dictionary<int, int> ListeningScoreMap = new()
{
    {0,5},{1,5},{2,5},{3,5},{4,10},{5,15},{6,20},{7,30},{8,35},{9,40},{10,45},
    {11,50},{12,55},{13,60},{14,65},{15,70},{16,75},{17,80},{18,85},{19,90},{20,95},
    {21,100},{22,105},{23,110},{24,115},{25,120},{26,125},{27,130},{28,135},{29,140},{30,145},
    {31,150},{32,155},{33,160},{34,165},{35,170},{36,175},{37,180},{38,185},{39,190},{40,195},
    {41,200},{42,205},{43,210},{44,215},{45,220},{46,225},{47,230},{48,235},{49,240},{50,245},
    {51,250},{52,255},{53,260},{54,265},{55,270},{56,275},{57,280},{58,285},{59,290},{60,295},
    {61,300},{62,305},{63,310},{64,315},{65,320},{66,325},{67,330},{68,335},{69,340},{70,345},
    {71,350},{72,355},{73,360},{74,365},{75,370},{76,375},{77,380},{78,385},{79,390},{80,395},
    {81,400},{82,405},{83,410},{84,415},{85,420},{86,425},{87,430},{88,435},{89,440},{90,445},
    {91,450},{92,455},{93,460},{94,465},{95,470},{96,475},{97,480},{98,485},{99,490},{100,495}
};

        private static readonly Dictionary<int, int> ReadingScoreMap = new()
{
    {0,5},{1,5},{2,5},{3,10},{4,15},{5,20},{6,25},{7,30},{8,35},{9,40},{10,45},
    {11,50},{12,55},{13,60},{14,65},{15,70},{16,75},{17,80},{18,85},{19,90},{20,95},
    {21,100},{22,105},{23,110},{24,115},{25,120},{26,125},{27,130},{28,135},{29,140},{30,145},
    {31,150},{32,155},{33,160},{34,165},{35,170},{36,175},{37,180},{38,185},{39,190},{40,195},
    {41,200},{42,205},{43,210},{44,215},{45,220},{46,225},{47,230},{48,235},{49,240},{50,245},
    {51,250},{52,255},{53,260},{54,265},{55,270},{56,275},{57,280},{58,285},{59,290},{60,295},
    {61,300},{62,305},{63,310},{64,315},{65,320},{66,325},{67,330},{68,335},{69,340},{70,345},
    {71,350},{72,355},{73,360},{74,365},{75,370},{76,375},{77,380},{78,385},{79,390},{80,395},
    {81,400},{82,405},{83,410},{84,415},{85,420},{86,425},{87,430},{88,435},{89,440},{90,445},
    {91,450},{92,455},{93,460},{94,465},{95,470},{96,475},{97,480},{98,485},{99,490},{100,495}
};

        private int ConvertToTOEICScore(int correctAnswers, bool isListening)
        {
            if (correctAnswers < 0) correctAnswers = 0;
            if (correctAnswers > 100) correctAnswers = 100;

            return isListening
                ? ListeningScoreMap[correctAnswers]
                : ReadingScoreMap[correctAnswers];
        }
    }
}



