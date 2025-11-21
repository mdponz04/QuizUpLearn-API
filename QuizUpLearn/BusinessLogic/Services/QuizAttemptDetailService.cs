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

            // Cập nhật QuizAttempt
            attempt.AttemptType = "placement";
            attempt.CorrectAnswers = correctLisCount + correctReaCount;
            attempt.WrongAnswers = attempt.TotalQuestions - attempt.CorrectAnswers;
            attempt.Score = lisPoint + reaPoint;
            attempt.Accuracy = attempt.TotalQuestions > 0 ? (decimal)attempt.CorrectAnswers / attempt.TotalQuestions : 0;
            attempt.Status = "completed";
            attempt.TimeSpent = totalTimeSpent > 0 ? totalTimeSpent : (int?)null;

            await _attemptRepo.UpdateAsync(dto.AttemptId, attempt);

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

        private int ConvertToTOEICScore(int correctAnswers, bool isListening)
        {
            if (correctAnswers <= 0) return 5;
            if (correctAnswers > 100) return 495;

            // Bảng quy đổi điểm TOEIC theo số câu đúng
            var scoreMap = new Dictionary<int, (int Listening, int Reading)>
            {
                { 1, (5, 5) }, { 2, (5, 5) }, { 3, (5, 5) }, { 4, (5, 5) }, { 5, (5, 5) },
                { 6, (5, 5) }, { 7, (5, 5) }, { 8, (5, 5) }, { 9, (5, 5) }, { 10, (5, 5) },
                { 11, (5, 5) }, { 12, (5, 5) }, { 13, (5, 5) }, { 14, (5, 5) }, { 15, (5, 5) },
                { 16, (5, 5) }, { 17, (5, 5) }, { 18, (10, 5) }, { 19, (15, 5) }, { 20, (20, 5) },
                { 21, (25, 5) }, { 22, (30, 10) }, { 23, (35, 15) }, { 24, (40, 20) }, { 25, (45, 25) },
                { 26, (50, 30) }, { 27, (55, 35) }, { 28, (60, 40) }, { 29, (70, 45) }, { 30, (80, 55) },
                { 31, (85, 60) }, { 32, (90, 65) }, { 33, (95, 70) }, { 34, (100, 75) }, { 35, (105, 80) },
                { 36, (115, 85) }, { 37, (125, 90) }, { 38, (135, 95) }, { 39, (140, 105) }, { 40, (150, 115) },
                { 41, (160, 120) }, { 42, (170, 125) }, { 43, (175, 130) }, { 44, (180, 135) }, { 45, (190, 140) },
                { 46, (200, 145) }, { 47, (205, 155) }, { 48, (215, 160) }, { 49, (220, 170) }, { 50, (225, 175) },
                { 51, (230, 185) }, { 52, (235, 195) }, { 53, (245, 205) }, { 54, (255, 210) }, { 55, (260, 215) },
                { 56, (265, 220) }, { 57, (275, 230) }, { 58, (285, 240) }, { 59, (290, 245) }, { 60, (295, 250) },
                { 61, (300, 255) }, { 62, (310, 260) }, { 63, (320, 270) }, { 64, (325, 275) }, { 65, (330, 280) },
                { 66, (335, 285) }, { 67, (340, 290) }, { 68, (345, 295) }, { 69, (350, 295) }, { 70, (355, 300) },
                { 71, (360, 310) }, { 72, (365, 315) }, { 73, (370, 320) }, { 74, (375, 325) }, { 75, (385, 330) },
                { 76, (395, 335) }, { 77, (400, 340) }, { 78, (405, 345) }, { 79, (415, 355) }, { 80, (420, 360) },
                { 81, (425, 370) }, { 82, (430, 375) }, { 83, (435, 385) }, { 84, (440, 390) }, { 85, (445, 395) },
                { 86, (455, 405) }, { 87, (460, 415) }, { 88, (465, 420) }, { 89, (475, 425) }, { 90, (480, 435) },
                { 91, (485, 440) }, { 92, (490, 450) }, { 93, (495, 455) }, { 94, (495, 460) }, { 95, (495, 470) },
                { 96, (495, 475) }, { 97, (495, 485) }, { 98, (495, 485) }, { 99, (495, 490) }, { 100, (495, 495) }
            };

            if (scoreMap.TryGetValue(correctAnswers, out var scores))
            {
                return isListening ? scores.Listening : scores.Reading;
            }

            // Nếu không tìm thấy, tính nội suy hoặc trả về giá trị gần nhất
            var lower = scoreMap.Keys.Where(k => k < correctAnswers).DefaultIfEmpty(1).Max();
            var upper = scoreMap.Keys.Where(k => k > correctAnswers).DefaultIfEmpty(100).Min();
            
            if (scoreMap.TryGetValue(lower, out var lowerScores) && scoreMap.TryGetValue(upper, out var upperScores))
            {
                var lowerScore = isListening ? lowerScores.Listening : lowerScores.Reading;
                var upperScore = isListening ? upperScores.Listening : upperScores.Reading;
                return lowerScore; // Trả về điểm thấp hơn (conservative)
            }

            return isListening ? 5 : 5;
        }
    }
}



