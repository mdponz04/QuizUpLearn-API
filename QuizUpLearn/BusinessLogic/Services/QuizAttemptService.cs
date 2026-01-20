using AutoMapper;
using BusinessLogic.DTOs;
using BusinessLogic.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Repository.Entities;
using Repository.Enums;
using Repository.Interfaces;
using System.Linq;

namespace BusinessLogic.Services
{
    public class QuizAttemptService : IQuizAttemptService
    {
        private readonly IQuizAttemptRepo _repo;
        private readonly IQuizAttemptDetailRepo _detailRepo;
        private readonly IQuizRepo _quizRepo;
        private readonly IUserMistakeRepo _userMistakeRepo;
        private readonly IUserMistakeService _userMistakeService;
        private readonly IQuizSetRepo _quizSetRepo;
        private readonly IQuizQuizSetRepo _quizQuizSetRepo;
        private readonly IAnswerOptionRepo _answerOptionRepo;
        private readonly ITournamentQuizSetRepo _tournamentQuizSetRepo;
        private readonly ITournamentParticipantRepo _tournamentParticipantRepo;
        private readonly IMapper _mapper;
        private readonly IServiceProvider _serviceProvider;

        public QuizAttemptService(
            IQuizAttemptRepo repo,
            IQuizAttemptDetailRepo detailRepo,
            IQuizRepo quizRepo,
            IUserMistakeRepo userMistakeRepo,
            IUserMistakeService userMistakeService,
            IQuizSetRepo quizSetRepo,
            IQuizQuizSetRepo quizQuizSetRepo,
            IAnswerOptionRepo answerOptionRepo,
            ITournamentQuizSetRepo tournamentQuizSetRepo,
            ITournamentParticipantRepo tournamentParticipantRepo,
            IMapper mapper,
            IServiceProvider serviceProvider)
        {
            _repo = repo;
            _detailRepo = detailRepo;
            _quizRepo = quizRepo;
            _userMistakeRepo = userMistakeRepo;
            _userMistakeService = userMistakeService;
            _quizSetRepo = quizSetRepo;
            _quizQuizSetRepo = quizQuizSetRepo;
            _answerOptionRepo = answerOptionRepo;
            _tournamentQuizSetRepo = tournamentQuizSetRepo;
            _tournamentParticipantRepo = tournamentParticipantRepo;
            _mapper = mapper;
            _serviceProvider = serviceProvider;
        }

        public async Task<ResponseQuizAttemptDto> CreateAsync(RequestQuizAttemptDto dto)
        {
            var entity = _mapper.Map<QuizAttempt>(dto);
            var created = await _repo.CreateAsync(entity);
            return _mapper.Map<ResponseQuizAttemptDto>(created);
        }

        public async Task<ResponseSingleStartDto> StartSingleAsync(RequestSingleStartDto dto)
        {
            // Fetch QuizSet với QuizGroupItems đã include
            var quizSet = await _quizSetRepo.GetQuizSetByIdAsync(dto.QuizSetId);
            if (quizSet == null)
            {
                throw new InvalidOperationException("Không tìm thấy bộ câu hỏi");
            }

            // Kiểm tra nếu đây là Placement Test - phải hoàn thành tất cả UserMistake trước
            if (quizSet.QuizSetType == QuizSetTypeEnum.Placement)
            {
                // Lấy danh sách UserMistake của user
                var userMistakes = await _userMistakeRepo.GetAlByUserIdAsync(dto.UserId);
                var mistakeList = userMistakes.ToList();

                // Kiểm tra xem user còn UserMistake nào không
                if (mistakeList.Any())
                {
                    throw new InvalidOperationException(
                        $"Bạn chưa hoàn thành việc xử lý các câu sai. " +
                        $"Vui lòng hoàn thành TẤT CẢ các câu sai trước khi làm Placement Test. " +
                        $"Bạn còn {mistakeList.Count} câu sai chưa xử lý.");
                }
            }

            // Kiểm tra xem quiz set này có thuộc tournament đang "Started" không
            var activeTournamentQuizSets = await _tournamentQuizSetRepo.GetActiveByQuizSetIdAsync(dto.QuizSetId);
            var activeTournamentQuizSetsList = activeTournamentQuizSets.ToList();
            var isTournamentQuiz = false; // Flag để xác định có phải tournament quiz không
            
            if (activeTournamentQuizSetsList.Any())
            {
                // Quiz set này thuộc tournament đang "Started"
                var today = DateTime.UtcNow.Date;
                
                foreach (var tournamentQuizSet in activeTournamentQuizSetsList)
                {
                    var tournament = tournamentQuizSet.Tournament;
                    
                    // Kiểm tra user đã join tournament này chưa
                    var participant = await _tournamentParticipantRepo.GetByTournamentAndUserAsync(tournament.Id, dto.UserId);
                    if (participant == null) continue; // User chưa join tournament này, bỏ qua
                    
                    // Kiểm tra ngày hiện tại có trong khoảng thời gian tournament không
                    if (today < tournament.StartDate.Date || today > tournament.EndDate.Date) continue;
                    
                    // Kiểm tra quiz set này có active cho ngày hôm nay không
                    if (tournamentQuizSet.UnlockDate.Date != today) continue;
                    
                    // Đánh dấu đây là tournament quiz
                    isTournamentQuiz = true;
                    
                    // Kiểm tra user đã có attempt completed trong ngày này chưa
                    // Check cả "single" và "tournament" để đảm bảo không có duplicate
                    var existingAttempts = await _repo.GetByQuizSetIdAsync(dto.QuizSetId, includeDeleted: false);
                    var todayAttempt = existingAttempts
                        .Where(a => 
                            a.UserId == dto.UserId
                            && (a.AttemptType == "single" || a.AttemptType == "tournament") // Chỉ check single và tournament
                            && a.Status == "completed"
                            && a.DeletedAt == null
                            && a.CreatedAt >= participant.JoinAt
                            && (a.UpdatedAt ?? a.CreatedAt).Date >= tournament.StartDate.Date
                            && (a.UpdatedAt ?? a.CreatedAt).Date <= tournament.EndDate.Date
                            && (a.UpdatedAt ?? a.CreatedAt).Date == today
                        )
                        .FirstOrDefault();
                    
                    if (todayAttempt != null)
                    {
                        throw new InvalidOperationException($"Bạn đã hoàn thành quiz của tournament này trong ngày hôm nay ({today:dd/MM/yyyy}). Mỗi ngày chỉ được làm 1 lần.");
                    }
                }
            }

            // Fetch ALL questions for the quiz set (no subset selection)
            var allQuestions = await _quizRepo.GetQuizzesByQuizSetIdAsync(dto.QuizSetId);
            var selected = allQuestions
                .OrderBy(q => q.OrderIndex ?? int.MaxValue)
                .ThenBy(q => q.CreatedAt)
                .ToList();

            // Lấy QuizGroupItems trực tiếp từ QuizSet (đã include sẵn)
            /*var quizGroupItems = quizSet.QuizGroupItems?.Where(qgi => qgi.DeletedAt == null).ToList() ?? new List<QuizGroupItem>();*/
            /*var quizGroupItemDtos = _mapper.Map<IEnumerable<BusinessLogic.DTOs.QuizGroupItemDtos.ResponseQuizGroupItemDto>>(quizGroupItems).ToList();*/

            // Create attempt in progress
            // Set AttemptType = "tournament" nếu đây là tournament quiz, ngược lại là "single"
            var attempt = new QuizAttempt
            {
                UserId = dto.UserId,
                QuizSetId = dto.QuizSetId,
                AttemptType = isTournamentQuiz ? "tournament" : "single",
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

            // Map to QuizStartResponseDto using mapper
            var quizDtos = _mapper.Map<IEnumerable<BusinessLogic.DTOs.QuizDtos.QuizStartResponseDto>>(selected).ToList();

            return new ResponseSingleStartDto
            {
                AttemptId = created.Id,
                TotalQuestions = created.TotalQuestions,
                Questions = quizDtos,
                //QuizGroupItems = quizGroupItemDtos
            };
        }

        public async Task<ResponseSingleStartDto> StartMistakeQuizzesAsync(RequestStartMistakeQuizzesDto dto)
        {
            if (dto.UserId == Guid.Empty)
            {
                throw new InvalidOperationException("Yêu cầu UserId");
            }

            // Cleanup WeakPoint orphan (không còn UserMistake liên quan) trước khi start
            await _userMistakeService.CleanupOrphanWeakPointsAsync(dto.UserId);

            // Lấy danh sách UserMistake của user
            var userMistakes = await _userMistakeRepo.GetAlByUserIdAsync(dto.UserId);
            var mistakeList = userMistakes.ToList();


            var unanalyzedMistakes = mistakeList.Where(um => !um.IsAnalyzed).ToList();
            if (unanalyzedMistakes.Any())
            {
                throw new InvalidOperationException(
                    $"Vui lòng đợi AI phân tích xong TẤT CẢ các câu sai trước khi làm lại. " +
                    $"Còn {unanalyzedMistakes.Count} câu chưa được phân tích.");
            }

            var quizIds = mistakeList
                .Select(um => um.QuizId)
                .Distinct()
                .ToList();

            if (!quizIds.Any())
            {
                throw new InvalidOperationException("Không tìm thấy câu hỏi sai nào cho người dùng này");
            }

            var quizzes = await _quizRepo.GetQuizzesByIdsAsync(quizIds);
            var selected = quizzes
                .Where(q => q.DeletedAt == null && q.IsActive)
                .OrderBy(q => q.OrderIndex ?? int.MaxValue)
                .ThenBy(q => q.CreatedAt)
                .ToList();

            if (!selected.Any())
            {
                throw new InvalidOperationException("Không tìm thấy câu hỏi hợp lệ nào cho người dùng này");
            }

            // Lấy QuizSetId từ quiz đầu tiên (thông qua QuizQuizSet)
            var firstQuiz = selected.First();
            var quizQuizSets = await _quizQuizSetRepo.GetByQuizIdAsync(firstQuiz.Id, includeDeleted: false);
            var quizQuizSetList = quizQuizSets.ToList();
            
            Guid quizSetId;
            if (quizQuizSetList.Any())
            {
                // Lấy QuizSetId từ QuizQuizSet đầu tiên
                quizSetId = quizQuizSetList.First().QuizSetId;
            }
            else
            {
                // Nếu quiz không có QuizSetId, tạo một QuizSet mới cho mistake quizzes
                // Hoặc có thể throw exception
                throw new InvalidOperationException($"Câu hỏi {firstQuiz.Id} không thuộc bộ câu hỏi nào");
            }

            var attempt = new QuizAttempt
            {
                UserId = dto.UserId,
                QuizSetId = quizSetId,
                AttemptType = "mistake_quiz",
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

            // Map sang QuizStartResponseDto để client hiển thị câu hỏi + đáp án
            var quizDtos = _mapper.Map<IEnumerable<BusinessLogic.DTOs.QuizDtos.QuizStartResponseDto>>(selected).ToList();

            return new ResponseSingleStartDto
            {
                AttemptId = created.Id,
                TotalQuestions = created.TotalQuestions,
                Questions = quizDtos,
                QuizGroupItems = new List<BusinessLogic.DTOs.QuizGroupItemDtos.ResponseQuizGroupItemDto>()
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

            // Kiểm tra validation cho tournament (áp dụng cho AttemptType = "single" hoặc "tournament" và quiz set thuộc tournament)
            if (attempt.AttemptType == "single" || attempt.AttemptType == "tournament")
            {
                var activeTournamentQuizSets = await _tournamentQuizSetRepo.GetActiveByQuizSetIdAsync(attempt.QuizSetId);
                var activeTournamentQuizSetsList = activeTournamentQuizSets.ToList();
                
                if (activeTournamentQuizSetsList.Any())
                {
                    var today = DateTime.UtcNow.Date;
                    
                    foreach (var tournamentQuizSet in activeTournamentQuizSetsList)
                    {
                        var tournament = tournamentQuizSet.Tournament;
                        
                        // Kiểm tra user đã join tournament này chưa
                        var participant = await _tournamentParticipantRepo.GetByTournamentAndUserAsync(tournament.Id, attempt.UserId);
                        if (participant == null) continue; // User chưa join tournament này, bỏ qua
                        
                        // Kiểm tra ngày hiện tại có trong khoảng thời gian tournament không
                        if (today < tournament.StartDate.Date || today > tournament.EndDate.Date) continue;
                        
                        // Kiểm tra quiz set này có active cho ngày hôm nay không
                        if (tournamentQuizSet.UnlockDate.Date != today) continue;
                        
                        // Kiểm tra user đã có attempt completed khác trong ngày này chưa (trừ attempt hiện tại)
                        // Check cả "single" và "tournament" để đảm bảo không có duplicate
                        var existingAttempts = await _repo.GetByQuizSetIdAsync(attempt.QuizSetId, includeDeleted: false);
                        var todayAttempt = existingAttempts
                            .Where(a => 
                                a.Id != id // Trừ attempt hiện tại
                                && a.UserId == attempt.UserId
                                && (a.AttemptType == "single" || a.AttemptType == "tournament") // Chỉ check single và tournament
                                && a.Status == "completed"
                                && a.DeletedAt == null
                                && a.CreatedAt >= participant.JoinAt
                                && (a.UpdatedAt ?? a.CreatedAt).Date >= tournament.StartDate.Date
                                && (a.UpdatedAt ?? a.CreatedAt).Date <= tournament.EndDate.Date
                                && (a.UpdatedAt ?? a.CreatedAt).Date == today
                            )
                            .FirstOrDefault();
                        
                        if (todayAttempt != null)
                        {
                            throw new InvalidOperationException($"Bạn đã hoàn thành quiz của tournament này trong ngày hôm nay ({today:dd/MM/yyyy}). Mỗi ngày chỉ được làm 1 lần.");
                        }
                    }
                }
            }

            var details = await _detailRepo.GetByAttemptIdAsync(id, includeDeleted: false);
            var detailList = details.ToList();
            int correct = 0;
            int wrong = 0;
            int score = 0;
            
            foreach (var d in detailList)
            {
                // Parse UserAnswer (AnswerOptionId) thành Guid
                bool isCorrect = false;
                if (Guid.TryParse(d.UserAnswer, out Guid answerOptionId))
                {
                    // Lấy AnswerOption dựa trên ID
                    var selectedAnswerOption = await _answerOptionRepo.GetByIdAsync(answerOptionId);
                    
                    if (selectedAnswerOption != null)
                    {
                        // Kiểm tra AnswerOption có thuộc về Quiz này không
                        if (selectedAnswerOption.QuizId == d.QuestionId)
                        {
                            isCorrect = selectedAnswerOption.IsCorrect;
                        }
                    }
                }
                
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
            
            // Check và assign badges (chạy background, không block response)
            if (updatedAttempt != null)
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        using var scope = _serviceProvider.CreateScope();
                        var badgeService = scope.ServiceProvider.GetRequiredService<IBadgeService>();
                        await badgeService.CheckAndAssignBadgesAsync(attempt.UserId);
                    }
                    catch (Exception ex)
                    {
                        // Log error nhưng không throw
                        // Có thể inject ILogger nếu cần
                    }
                });
            }
            
            return updatedAttempt == null ? null : _mapper.Map<ResponseQuizAttemptDto>(updatedAttempt);
        }

        public async Task<PlayerHistoryResponseDto> GetPlayerHistoryAsync(PlayerHistoryRequestDto request)
        {
            // Use optimized repository method with pagination at database level
            var (attempts, totalCount) = await _repo.GetUserHistoryPagedAsync(
                request.UserId,
                request.QuizSetId,
                request.Status,
                request.AttemptType,
                request.SortBy,
                request.SortOrder,
                request.Page,
                request.PageSize,
                includeDeleted: false);

            var mapped = _mapper.Map<List<ResponseQuizAttemptDto>>(attempts);

            foreach (var a in mapped)
            {
                var type = (a.AttemptType ?? string.Empty).Trim().ToLowerInvariant();

                if (type == "single" || type == "multi" || type == "multiplayer" || type == "1v1" || type == "onevsone")
                {
                    // keep mapped QuizSetName (QuizSet.Title)
                    a.QuizSetName ??= "Đề luyện tập";
                }
                else if (type == "event")
                {
                    a.QuizSetName = "Đề sự kiện";
                }
                else if (type == "tournament")
                {
                    a.QuizSetName = "Đề giải đấu";
                }
                else if (type == "placement")
                {
                    a.QuizSetName = "Đề đầu vào";
                }
                else if (type == "mistake_quiz")
                {
                    a.QuizSetName = "Đề chữa sai";
                }
                else
                {
                    a.QuizSetName = "Đề khác";
                }
            }

            return new PlayerHistoryResponseDto
            {
                Attempts = mapped,
                TotalCount = totalCount,
                Page = request.Page,
                PageSize = request.PageSize
            };
        }

        public async Task<PlacementTestHistoryResponseDto> GetPlacementTestHistoryAsync(PlayerHistoryRequestDto request)
        {
            // Use optimized repository method with pagination at database level
            // This method already includes QuizAttemptDetails and Quiz to avoid N+1 queries
            var (attempts, totalCount) = await _repo.GetPlacementTestHistoryPagedAsync(
                request.UserId,
                request.QuizSetId,
                request.Status,
                request.SortBy,
                request.SortOrder,
                request.Page,
                request.PageSize,
                includeDeleted: false);

            var historyItems = new List<PlacementTestHistoryItemDto>();

            // Process attempts - QuizAttemptDetails and Quiz are already loaded via Include
            foreach (var attempt in attempts)
            {
                int correctLisCount = 0;
                int correctReaCount = 0;

                // Details are already loaded via Include, no need to query again
                var details = attempt.QuizAttemptDetails?.Where(d => d.DeletedAt == null) ?? Enumerable.Empty<QuizAttemptDetail>();

                foreach (var detail in details)
                {
                    if (detail.IsCorrect == true && detail.Quiz != null)
                    {
                        // Quiz is already loaded via Include, no need to query again
                        var isListening = detail.Quiz.TOEICPart == "PART1" || detail.Quiz.TOEICPart == "PART2" ||
                                        detail.Quiz.TOEICPart == "PART3" || detail.Quiz.TOEICPart == "PART4";
                        
                        if (isListening)
                            correctLisCount++;
                        else
                            correctReaCount++;
                    }
                }

                // Tính LisPoint và ReaPoint
                int lisPoint = ConvertToTOEICScore(correctLisCount, isListening: true);
                int reaPoint = ConvertToTOEICScore(correctReaCount, isListening: false);

                historyItems.Add(new PlacementTestHistoryItemDto
                {
                    Id = attempt.Id,
                    UserId = attempt.UserId,
                    QuizSetId = attempt.QuizSetId,
                    AttemptType = attempt.AttemptType,
                    TotalQuestions = attempt.TotalQuestions,
                    CorrectAnswers = attempt.CorrectAnswers,
                    WrongAnswers = attempt.WrongAnswers,
                    Score = attempt.Score,
                    Accuracy = attempt.Accuracy,
                    TimeSpent = attempt.TimeSpent,
                    Status = attempt.Status,
                    CreatedAt = attempt.CreatedAt,
                    UpdatedAt = attempt.UpdatedAt,
                    LisPoint = lisPoint,
                    TotalCorrectLisAns = correctLisCount,
                    ReaPoint = reaPoint,
                    TotalCorrectReaAns = correctReaCount
                });
            }

            return new PlacementTestHistoryResponseDto
            {
                Attempts = historyItems,
                TotalCount = totalCount,
                Page = request.Page,
                PageSize = request.PageSize
            };
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
