using AutoMapper;
using BusinessLogic.DTOs;
using BusinessLogic.DTOs.UserMistakeDtos;
using BusinessLogic.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Repository.Entities;
using Repository.Enums;
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
        private readonly IUserRepo _userRepo;

        public QuizAttemptDetailService(
            IQuizAttemptDetailRepo repo,
            IQuizAttemptRepo attemptRepo,
            IAnswerOptionRepo answerOptionRepo,
            IQuizRepo quizRepo,
            IUserMistakeService userMistakeService,
            IUserMistakeRepo userMistakeRepo,
            IAIService aiService,
            IMapper mapper,
            IServiceScopeFactory scopeFactory,
            IUserRepo userRepo)
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
            _userRepo = userRepo;
        }

        public async Task<ResponseQuizAttemptDetailDto> CreateAsync(RequestQuizAttemptDetailDto dto)
        {
            var entity = _mapper.Map<QuizAttemptDetail>(dto);
            
            // Nếu OrderIndex chưa được set, lấy từ Quiz
            if (!entity.OrderIndex.HasValue)
            {
                var quiz = await _quizRepo.GetByIdAsync(dto.QuestionId);
                if (quiz != null)
                {
                    entity.OrderIndex = quiz.OrderIndex;
                }
            }
            
            // Nếu IsCorrect chưa được set, tự động tính từ UserAnswer
            if (!entity.IsCorrect.HasValue && !string.IsNullOrWhiteSpace(dto.UserAnswer))
            {
                if (Guid.TryParse(dto.UserAnswer, out Guid selectedAnswerOptionId))
                {
                    var selectedAnswerOption = await _answerOptionRepo.GetByIdAsync(selectedAnswerOptionId);
                    if (selectedAnswerOption != null && selectedAnswerOption.QuizId == dto.QuestionId)
                    {
                        entity.IsCorrect = selectedAnswerOption.IsCorrect;
                    }
                }
            }
            
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

        public async Task<PaginationResponseDto<ResponseQuizAttemptDetailExtendedDto>> GetByAttemptIdPagedAsync(
            Guid attemptId, 
            PaginationRequestDto pagination, 
            bool includeDeleted = false)
        {
            pagination ??= new PaginationRequestDto();
            
            var (details, totalCount) = await _repo.GetByAttemptIdPagedAsync(
                attemptId, 
                pagination.Page, 
                pagination.PageSize, 
                includeDeleted);

            var detailList = details.ToList();
            var extendedDtos = new List<ResponseQuizAttemptDetailExtendedDto>();

            foreach (var detail in detailList)
            {
                var dto = new ResponseQuizAttemptDetailExtendedDto
                {
                    Id = detail.Id,
                    AttemptId = detail.AttemptId,
                    QuestionId = detail.QuestionId,
                    QuestionText = detail.Quiz?.QuestionText ?? string.Empty,
                    UserAnswer = detail.UserAnswer,
                    IsCorrect = detail.IsCorrect,
                    TimeSpent = detail.TimeSpent,
                    CreatedAt = detail.CreatedAt,
                    UpdatedAt = detail.UpdatedAt,
                    DeletedAt = detail.DeletedAt,
                    AudioURL = detail.Quiz?.AudioURL,
                    ImageURL = detail.Quiz?.ImageURL,
                    QuizGroupItemId = detail.Quiz?.QuizGroupItemId
                };

                // Get QuizSet name only if QuizSetType is Practice (0)
                if (detail.QuizAttempt?.QuizSet != null && 
                    detail.QuizAttempt.QuizSet.QuizSetType == QuizSetTypeEnum.Practice)
                {
                    dto.QuizSetName = detail.QuizAttempt.QuizSet.Title;
                }

                // Get UserAnswerText from AnswerOption if UserAnswer is a Guid
                if (!string.IsNullOrWhiteSpace(detail.UserAnswer) && 
                    Guid.TryParse(detail.UserAnswer, out Guid answerOptionId))
                {
                    var answerOption = detail.Quiz?.AnswerOptions?
                        .FirstOrDefault(ao => ao.Id == answerOptionId);
                    if (answerOption != null)
                    {
                        dto.UserAnswerText = answerOption.OptionText;
                    }
                }

                extendedDtos.Add(dto);
            }

            return PaginationResponseDto<ResponseQuizAttemptDetailExtendedDto>.Create(
                pagination, 
                totalCount, 
                extendedDtos);
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

        // Chấm điểm bài làm thông thường (không phải flow MistakeQuiz)
        public async Task<ResponseSubmitAnswersDto> SubmitAnswersAsync(RequestSubmitAnswersDto dto)
        {
            var attempt = await _attemptRepo.GetByIdAsync(dto.AttemptId);
            if (attempt == null)
            {
                throw new InvalidOperationException("Attempt not found");
            }

            // Tối ưu: Load tất cả Quiz và AnswerOptions một lần thay vì N lần query
            var questionIds = dto.Answers.Select(a => a.QuestionId).Distinct().ToList();
            var allQuizzes = await _quizRepo.GetQuizzesByIdsAsync(questionIds);
            var quizDict = allQuizzes.ToDictionary(q => q.Id);

            // Tạo dictionary cho AnswerOptions để lookup nhanh (O(1))
            var answerOptionDict = new Dictionary<Guid, AnswerOption>();
            var answerOptionsByQuizId = new Dictionary<Guid, List<AnswerOption>>();
            foreach (var quiz in allQuizzes)
            {
                if (quiz.AnswerOptions != null)
                {
                    var optionsList = quiz.AnswerOptions.ToList();
                    answerOptionsByQuizId[quiz.Id] = optionsList;
                    foreach (var option in optionsList)
                    {
                        answerOptionDict[option.Id] = option;
                    }
                }
            }

            int correctCount = 0;
            int wrongCount = 0;
            int totalTimeSpent = 0;
            var answerResults = new List<AnswerResultDto>();
            var wrongQuestionIdsSet = new HashSet<Guid>();
            var wrongAnswersByQuestion = new Dictionary<Guid, string>();
            var detailsToInsert = new List<QuizAttemptDetail>();

            // Lưu và chấm điểm từng câu trả lời (xử lý trong memory, không query DB)
            foreach (var answer in dto.Answers)
            {
                // Kiểm tra đáp án đúng
                bool isCorrect = false;
                Guid? correctAnswerOptionId = null;

                if (Guid.TryParse(answer.UserAnswer, out Guid selectedAnswerOptionId))
                {
                    // Lookup AnswerOption từ dictionary (O(1) lookup)
                    if (answerOptionDict.TryGetValue(selectedAnswerOptionId, out var selectedAnswerOption))
                    {
                        if (selectedAnswerOption.QuizId == answer.QuestionId)
                        {
                            isCorrect = selectedAnswerOption.IsCorrect;
                            
                            // Tìm đáp án đúng (nếu người dùng chọn sai)
                            if (!isCorrect)
                            {
                                // Lookup từ dictionary thay vì query DB
                                if (answerOptionsByQuizId.TryGetValue(answer.QuestionId, out var answerOptions))
                                {
                                    var correctOption = answerOptions.FirstOrDefault(ao => ao.IsCorrect);
                                    correctAnswerOptionId = correctOption?.Id;
                                }
                            }
                        }
                    }
                }
                else
                {
                    // Nếu không parse được, tìm đáp án đúng từ dictionary
                    if (answerOptionsByQuizId.TryGetValue(answer.QuestionId, out var answerOptions))
                    {
                        var correctOption = answerOptions.FirstOrDefault(ao => ao.IsCorrect);
                        correctAnswerOptionId = correctOption?.Id;
                    }
                }

                // Tạo QuizAttemptDetail (chưa insert)
                var detail = new QuizAttemptDetail
                {
                    AttemptId = dto.AttemptId,
                    QuestionId = answer.QuestionId,
                    UserAnswer = answer.UserAnswer ?? string.Empty,
                    IsCorrect = isCorrect,
                    TimeSpent = answer.TimeSpent,
                    QuizId = answer.QuestionId,
                    QuizAttemptId = dto.AttemptId,
                    OrderIndex = quizDict.TryGetValue(answer.QuestionId, out var quiz) ? quiz.OrderIndex : null
                };

                detailsToInsert.Add(detail);
                
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
                    wrongAnswersByQuestion[answer.QuestionId] = answer.UserAnswer ?? string.Empty;
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

            // Batch insert tất cả QuizAttemptDetail một lần (thay vì N lần SaveChanges)
            await _repo.CreateBatchAsync(detailsToInsert);

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

            if (attempt.Status == "completed")
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        using var scope = _scopeFactory.CreateScope();
                        var badgeService = scope.ServiceProvider.GetRequiredService<IBadgeService>();
                        await badgeService.CheckAndAssignBadgesAsync(attempt.UserId);
                    }
                    catch (Exception ex)
                    {
                        ex.Message.ToString();
                    }
                });
            }

            return response;
        }

        /// <summary>
        /// Chấm điểm bài làm lại các câu sai (MistakeQuiz) và xoá UserMistake cho các câu đã trả lời đúng
        /// </summary>
        public async Task<ResponseSubmitAnswersDto> SubmitMistakeQuizAnswersAsync(RequestSubmitAnswersDto dto)
        {
            var attempt = await _attemptRepo.GetByIdAsync(dto.AttemptId);
            if (attempt == null)
            {
                throw new InvalidOperationException("Attempt not found");
            }

            // Validate AttemptType phải là "mistake_quiz"
            if (attempt.AttemptType != "mistake_quiz")
            {
                throw new InvalidOperationException($"This attempt is not a mistake quiz. AttemptType: {attempt.AttemptType}");
            }

            int correctCount = 0;
            int wrongCount = 0;
            int totalTimeSpent = 0;
            var answerResults = new List<AnswerResultDto>();
            var wrongQuestionIdsSet = new HashSet<Guid>();
            var correctQuestionIdsSet = new HashSet<Guid>();
            var wrongAnswersByQuestion = new Dictionary<Guid, string>();

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
                    correctQuestionIdsSet.Add(answer.QuestionId);
                }
                else
                {
                    wrongQuestionIdsSet.Add(answer.QuestionId);
                    wrongAnswersByQuestion[answer.QuestionId] = answer.UserAnswer ?? string.Empty;
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

            // Cập nhật QuizAttempt với kết quả (đảm bảo AttemptType vẫn là "mistake_quiz")
            attempt.AttemptType = "mistake_quiz"; // Đảm bảo không bị thay đổi thành "placement" hoặc type khác
            attempt.CorrectAnswers = correctCount;
            attempt.WrongAnswers = wrongCount;
            attempt.Score = correctCount;
            attempt.Accuracy = attempt.TotalQuestions > 0 ? (decimal)correctCount / attempt.TotalQuestions : 0;
            attempt.Status = "completed";
            attempt.IsCompleted = true;
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

            // Khôi phục: Xoá UserMistake/UserWeakPoint cho các câu đã làm đúng trong MistakeQuiz
            var correctQuestionIds = correctQuestionIdsSet.ToList();
            var userId = attempt.UserId;

            if (userId != Guid.Empty && correctQuestionIds.Any())
            {
                using var scope = _scopeFactory.CreateScope();
                var userMistakeRepo = scope.ServiceProvider.GetRequiredService<IUserMistakeRepo>();
                var userMistakeService = scope.ServiceProvider.GetRequiredService<IUserMistakeService>();
                var userWeakPointService = scope.ServiceProvider.GetRequiredService<IUserWeakPointService>();

                // Bước 1: Thu thập UserMistake cần xoá và WeakPoint liên quan
                var mistakesToDelete = new List<UserMistake>();
                var weakPointIdsToCheck = new HashSet<Guid>();

                foreach (var quizId in correctQuestionIds)
                {
                    var existingMistake = await userMistakeRepo.GetByUserIdAndQuizIdAsync(userId, quizId);
                    if (existingMistake != null)
                    {
                        mistakesToDelete.Add(existingMistake);
                        if (existingMistake.UserWeakPointId.HasValue)
                        {
                            weakPointIdsToCheck.Add(existingMistake.UserWeakPointId.Value);
                        }
                    }
                }

                // Bước 2: Xoá UserMistake
                foreach (var mistake in mistakesToDelete)
                {
                    await userMistakeService.DeleteAsync(mistake.Id);
                }

                // Bước 3: Xoá UserWeakPoint nếu không còn UserMistake liên kết
                foreach (var weakPointId in weakPointIdsToCheck)
                {
                    var remainingMistakes = await userMistakeRepo.GetByUserWeakPointIdAsync(weakPointId);
                    if (!remainingMistakes.Any())
                    {
                        try
                        {
                            await userWeakPointService.DeleteAsync(weakPointId);
                        }
                        catch
                        {
                        }
                    }
                }
            }

            if (attempt.Status == "completed")
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        using var scope = _scopeFactory.CreateScope();
                        var badgeService = scope.ServiceProvider.GetRequiredService<IBadgeService>();
                        await badgeService.CheckAndAssignBadgesAsync(attempt.UserId);
                    }
                    catch (Exception ex)
                    {
                        // Log error nếu cần
                    }
                });
            }

            return response;
        }

        public async Task<ResponsePlacementTestDto> SubmitPlacementTestAsync(RequestSubmitAnswersDto dto)
        {
            var attempt = await _attemptRepo.GetByIdAsync(dto.AttemptId);
            if (attempt == null)
            {
                throw new InvalidOperationException("Attempt not found");
            }

            // Tối ưu: Load tất cả Quiz và AnswerOptions một lần thay vì 200 lần query
            var questionIds = dto.Answers.Select(a => a.QuestionId).Distinct().ToList();
            var allQuizzes = await _quizRepo.GetQuizzesByIdsAsync(questionIds);
            var quizDict = allQuizzes.ToDictionary(q => q.Id);

            // Tạo dictionary cho AnswerOptions để lookup nhanh
            var answerOptionDict = new Dictionary<Guid, AnswerOption>();
            foreach (var quiz in allQuizzes)
            {
                if (quiz.AnswerOptions != null)
                {
                    foreach (var option in quiz.AnswerOptions)
                    {
                        answerOptionDict[option.Id] = option;
                    }
                }
            }

            int totalTimeSpent = 0;
            int correctLisCount = 0;
            int correctReaCount = 0;
            var wrongQuestionIdsSet = new HashSet<Guid>();
            var wrongAnswersByQuestion = new Dictionary<Guid, string>();
            var detailsToInsert = new List<QuizAttemptDetail>();

            // Lưu và chấm điểm từng câu trả lời (xử lý trong memory, không query DB)
            foreach (var answer in dto.Answers)
            {
                // Lấy thông tin Quiz từ dictionary (O(1) lookup)
                if (!quizDict.TryGetValue(answer.QuestionId, out var quiz) || quiz == null)
                {
                    continue;
                }

                // Kiểm tra user có chọn đáp án hay không
                bool hasAnswer = !string.IsNullOrWhiteSpace(answer.UserAnswer);
                bool isCorrect = false;

                if (hasAnswer && Guid.TryParse(answer.UserAnswer, out Guid selectedAnswerOptionId))
                {
                    // Lookup AnswerOption từ dictionary (O(1) lookup)
                    if (answerOptionDict.TryGetValue(selectedAnswerOptionId, out var selectedAnswerOption))
                    {
                        if (selectedAnswerOption.QuizId == answer.QuestionId)
                        {
                            isCorrect = selectedAnswerOption.IsCorrect;
                        }
                    }
                }
                // Nếu không chọn đáp án (null, empty, hoặc không parse được) → isCorrect = false

                // Tạo QuizAttemptDetail (chưa insert)
                var detail = new QuizAttemptDetail
                {
                    AttemptId = dto.AttemptId,
                    QuestionId = answer.QuestionId,
                    UserAnswer = answer.UserAnswer ?? string.Empty, // Đảm bảo không null
                    IsCorrect = isCorrect,
                    TimeSpent = answer.TimeSpent,
                    QuizId = answer.QuestionId,
                    QuizAttemptId = dto.AttemptId,
                    OrderIndex = quiz?.OrderIndex
                };

                detailsToInsert.Add(detail);

                if (answer.TimeSpent.HasValue)
                {
                    totalTimeSpent += answer.TimeSpent.Value;
                }

                // Phân loại Listening/Reading và đếm câu đúng
                // Normalize TOEICPart để tránh lệch do khoảng trắng / hoa-thường ("Part1", "PART1 ", " part1", ...)
                var normalizedPart = (quiz.TOEICPart ?? string.Empty).Trim().ToUpperInvariant();
                var isListening = normalizedPart == "PART1" || normalizedPart == "PART2" ||
                                  normalizedPart == "PART3" || normalizedPart == "PART4";
                
                if (isCorrect)
                {
                    if (isListening)
                        correctLisCount++;
                    else
                        correctReaCount++;
                }
                else
                {
                    // Chỉ thêm vào wrongQuestionIdsSet nếu user đã chọn đáp án (dù sai)
                    // Nếu không chọn đáp án, vẫn tính là sai nhưng có thể xử lý khác nếu cần
                    wrongQuestionIdsSet.Add(answer.QuestionId);
                    wrongAnswersByQuestion[answer.QuestionId] = answer.UserAnswer ?? string.Empty;
                }
            }

            // Batch insert tất cả QuizAttemptDetail một lần (thay vì 200 lần SaveChanges)
            await _repo.CreateBatchAsync(detailsToInsert);

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

            // Check và assign badges (chạy background, không block response)
            if (attempt.Status == "completed")
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        using var scope = _scopeFactory.CreateScope();
                        var badgeService = scope.ServiceProvider.GetRequiredService<IBadgeService>();
                        await badgeService.CheckAndAssignBadgesAsync(attempt.UserId);
                    }
                    catch (Exception ex)
                    {
                        // Log error nếu cần
                    }
                });
            }

            // Nền 0: Cập nhật User.TotalPoints nếu điểm placement test cao hơn điểm hiện tại (chạy song song với UserMistake)
            var userId = attempt.UserId;
            var totalPlacementScoreSnapshot = totalPlacementScore;
            _ = Task.Run(async () =>
            {
                if (userId == Guid.Empty)
                {
                    return;
                }

                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var userRepo = scope.ServiceProvider.GetRequiredService<IUserRepo>();
                    var user = await userRepo.GetByIdAsync(userId);
                    if (user != null && totalPlacementScoreSnapshot > user.TotalPoints)
                    {
                        user.TotalPoints = totalPlacementScoreSnapshot;
                        await userRepo.UpdateAsync(userId, user);
                    }
                }
                catch (Exception ex)
                {
                    // Log error for background User.TotalPoints update
                    // Could add logging here if needed
                }
            });

            // Nền 1: Tạo/update UserMistake cho các câu trả lời sai
            var wrongQuestionIds = wrongQuestionIdsSet.ToList();
            var wrongAnswersSnapshot = wrongAnswersByQuestion.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
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

                    // Tạo/Update UserMistake cho các câu trả lời sai
                    foreach (var quizId in wrongQuestionIds)
                    {
                        try
                        {
                            var existingMistake = await userMistakeRepo.GetByUserIdAndQuizIdAsync(userId, quizId);
                            wrongAnswersSnapshot.TryGetValue(quizId, out var userAnswer);
                            
                            // Đảm bảo UserAnswer không null (dùng string.Empty nếu null hoặc empty)
                            var safeUserAnswer = string.IsNullOrWhiteSpace(userAnswer) ? string.Empty : userAnswer;

                            if (existingMistake == null)
                            {
                                // Tạo mới UserMistake với đầy đủ field
                                await userMistakeService.AddAsync(new RequestUserMistakeDto
                                {
                                    UserId = userId,
                                    QuizId = quizId,
                                    TimesAttempted = 1,
                                    TimesWrong = 1,
                                    LastAttemptedAt = DateTime.UtcNow,
                                    IsAnalyzed = false,
                                    UserAnswer = safeUserAnswer
                                });
                            }
                            else
                            {
                                // Update UserMistake với đầy đủ field
                                // Lưu ý: UserAnswer chỉ update nếu có giá trị mới, không thì giữ nguyên giá trị cũ
                                await userMistakeService.UpdateAsync(existingMistake.Id, new RequestUserMistakeDto
                                {
                                    UserId = userId,
                                    QuizId = quizId,
                                    TimesAttempted = existingMistake.TimesAttempted + 1,
                                    TimesWrong = existingMistake.TimesWrong + 1,
                                    LastAttemptedAt = DateTime.UtcNow,
                                    IsAnalyzed = existingMistake.IsAnalyzed,
                                    UserAnswer = !string.IsNullOrWhiteSpace(safeUserAnswer) ? safeUserAnswer : existingMistake.UserAnswer
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
    {0,5},{1,15},{2,20},{3,25},{4,30},{5,35},{6,40},{7,45},{8,50},{9,55},
    {10,60},{11,65},{12,70},{13,75},{14,80},{15,85},{16,90},{17,95},{18,100},{19,105},
    {20,110},{21,115},{22,120},{23,125},{24,130},{25,135},{26,140},{27,145},{28,150},{29,155},
    {30,160},{31,165},{32,170},{33,175},{34,180},{35,185},{36,190},{37,195},{38,200},{39,205},
    {40,210},{41,215},{42,220},{43,225},{44,230},{45,235},{46,240},{47,245},{48,250},{49,255},
    {50,260},{51,265},{52,270},{53,275},{54,280},{55,285},{56,290},{57,295},{58,300},{59,305},
    {60,310},{61,315},{62,320},{63,325},{64,330},{65,335},{66,340},{67,345},{68,350},{69,355},
    {70,360},{71,365},{72,370},{73,375},{74,380},{75,385},{76,395},{77,400},{78,405},{79,410},
    {80,415},{81,420},{82,425},{83,430},{84,435},{85,440},{86,445},{87,450},{88,455},{89,460},
    {90,465},{91,470},{92,475},{93,480},{94,485},{95,490},{96,495},{97,495},{98,495},{99,495},{100,495}
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



