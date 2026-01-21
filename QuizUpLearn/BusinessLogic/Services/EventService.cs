using BusinessLogic.DTOs;
using BusinessLogic.DTOs.EventDtos;
using BusinessLogic.Interfaces;
using Repository.Entities;
using Repository.Enums;
using Repository.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using BusinessLogic.Helpers;

namespace BusinessLogic.Services
{
    public class EventService : IEventService
    {
        private readonly IEventRepo _eventRepo;
        private readonly IEventParticipantRepo _eventParticipantRepo;
        private readonly IQuizSetRepo _quizSetRepo;
        private readonly IQuizRepo _quizRepo;
        private readonly IUserRepo _userRepo;
        private readonly IAccountRepo _accountRepo;
        private readonly IRealtimeGameService _realtimeGameService;
        private readonly IMailerSendService _mailerSendService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<EventService> _logger;
        private readonly IQuizAttemptRepo _quizAttemptRepo;

        public EventService(
            IEventRepo eventRepo,
            IEventParticipantRepo eventParticipantRepo,
            IQuizSetRepo quizSetRepo,
            IQuizRepo quizRepo,
            IUserRepo userRepo,
            IAccountRepo accountRepo,
            IRealtimeGameService realtimeGameService,
            IMailerSendService mailerSendService,
            IConfiguration configuration,
            ILogger<EventService> logger,
            IQuizAttemptRepo quizAttemptRepo)
        {
            _eventRepo = eventRepo;
            _eventParticipantRepo = eventParticipantRepo;
            _quizSetRepo = quizSetRepo;
            _quizRepo = quizRepo;
            _userRepo = userRepo;
            _accountRepo = accountRepo;
            _realtimeGameService = realtimeGameService;
            _mailerSendService = mailerSendService;
            _configuration = configuration;
            _logger = logger;
            _quizAttemptRepo = quizAttemptRepo;
        }

        private FinalResultDto BuildFinalResultFromSession(string gamePin, GameSessionDto session)
        {
            var rankings = session.Players
                .OrderByDescending(p => p.Score)
                .Select((p, index) => new PlayerScore
                {
                    PlayerName = p.PlayerName,
                    TotalScore = p.Score,
                    CorrectAnswers = p.CorrectAnswers,
                    TotalAnswered = p.TotalAnswered,
                    Rank = index + 1
                })
                .ToList();

            return new FinalResultDto
            {
                GamePin = gamePin,
                FinalRankings = rankings,
                Winner = rankings.FirstOrDefault(),
                CompletedAt = DateTime.UtcNow,
                TotalPlayers = session.Players.Count,
                TotalQuestions = session.Questions.Count
            };
        }

        public async Task<EventResponseDto> CreateEventAsync(Guid userId, CreateEventRequestDto dto)
        {
            // Validate QuizSet exists
            var quizSet = await _quizSetRepo.GetQuizSetByIdAsync(dto.QuizSetId);
            if (quizSet == null)
                throw new ArgumentException("QuizSet không tồn tại");

            // ✅ VALIDATION: QuizSet phải có QuizSetType = Event
            if (quizSet.QuizSetType != QuizSetTypeEnum.Event)
                throw new ArgumentException("QuizSet phải có QuizSetType là Event để tạo Event");

            // ✅ VALIDATION: QuizSet phải có ít nhất 1 câu hỏi
            var quizzes = await _quizRepo.GetQuizzesByQuizSetIdAsync(dto.QuizSetId);
            if (quizzes == null || !quizzes.Any())
                throw new ArgumentException("QuizSet của Event chưa có câu hỏi. Vui lòng thêm câu hỏi trước khi tạo Event.");

            // Validate dates
            if (dto.StartDate >= dto.EndDate)
                throw new ArgumentException("StartDate phải trước EndDate");

            if (dto.StartDate < DateTime.UtcNow)
                throw new ArgumentException("StartDate không thể là thời điểm trong quá khứ");

            var entity = new Event
            {
                Id = Guid.NewGuid(),
                QuizSetId = dto.QuizSetId,
                Name = dto.Name,
                Description = dto.Description,
                StartDate = dto.StartDate.ToUniversalTime(),
                EndDate = dto.EndDate.ToUniversalTime(),
                MaxParticipants = dto.MaxParticipants,
                Status = "Upcoming", // Mặc định là Upcoming
                CreatedBy = userId,
                CreatedAt = DateTime.UtcNow
            };

            var created = await _eventRepo.CreateAsync(entity);
            
            _logger.LogInformation($"✅ Event created: {created.Name} (ID: {created.Id}) by User {userId}");

            return await MapToResponseDto(created);
        }

        public async Task<EventResponseDto?> GetEventByIdAsync(Guid id)
        {
            var entity = await _eventRepo.GetByIdWithDetailsAsync(id);
            if (entity == null)
                return null;

            return await MapToResponseDto(entity);
        }

        public async Task<IEnumerable<EventResponseDto>> GetAllEventsAsync()
        {
            var entities = await _eventRepo.GetAllAsync();
            var result = new List<EventResponseDto>();

            foreach (var entity in entities)
            {
                result.Add(await MapToResponseDto(entity));
            }

            return result;
        }

        public async Task<IEnumerable<EventResponseDto>> GetActiveEventsAsync()
        {
            var entities = await _eventRepo.GetActiveEventsAsync();
            var result = new List<EventResponseDto>();

            foreach (var entity in entities)
            {
                result.Add(await MapToResponseDto(entity));
            }

            return result;
        }

        public async Task<IEnumerable<EventResponseDto>> GetUpcomingEventsAsync()
        {
            var entities = await _eventRepo.GetUpcomingEventsAsync();
            var result = new List<EventResponseDto>();

            foreach (var entity in entities)
            {
                result.Add(await MapToResponseDto(entity));
            }

            return result;
        }

        public async Task<IEnumerable<EventResponseDto>> GetMyEventsAsync(Guid userId)
        {
            var entities = await _eventRepo.GetEventsByCreatorAsync(userId);
            var result = new List<EventResponseDto>();

            foreach (var entity in entities)
            {
                result.Add(await MapToResponseDto(entity));
            }

            return result;
        }

        public async Task<EventResponseDto?> UpdateEventAsync(Guid id, UpdateEventRequestDto dto)
        {
            var entity = await _eventRepo.GetByIdAsync(id);
            if (entity == null)
                return null;

            // Không cho phép update nếu event đã Active hoặc Completed
            if (entity.Status == "Active" || entity.Status == "Completed")
                throw new InvalidOperationException("Không thể cập nhật Event đang Active hoặc đã Completed");

            // Update fields if provided
            if (!string.IsNullOrEmpty(dto.Name))
                entity.Name = dto.Name;

            if (!string.IsNullOrEmpty(dto.Description))
                entity.Description = dto.Description;

            if (dto.StartDate.HasValue)
            {
                if (dto.StartDate.Value < DateTime.UtcNow)
                    throw new ArgumentException("StartDate không thể là thời điểm trong quá khứ");
                entity.StartDate = dto.StartDate.Value.ToUniversalTime();
            }

            if (dto.EndDate.HasValue)
                entity.EndDate = dto.EndDate.Value.ToUniversalTime();

            if (dto.MaxParticipants.HasValue)
                entity.MaxParticipants = dto.MaxParticipants.Value;

            if (!string.IsNullOrEmpty(dto.Status))
                entity.Status = dto.Status;

            // Validate dates
            if (entity.StartDate >= entity.EndDate)
                throw new ArgumentException("StartDate phải trước EndDate");

            var updated = await _eventRepo.UpdateAsync(entity);
            return await MapToResponseDto(updated);
        }

        public async Task<bool> DeleteEventAsync(Guid id)
        {
            var entity = await _eventRepo.GetByIdAsync(id);
            if (entity == null)
                return false;

            // Không cho phép xóa nếu event đã Active
            if (entity.Status == "Active")
                throw new InvalidOperationException("Không thể xóa Event đang Active");

            return await _eventRepo.DeleteAsync(id);
        }

        /// <summary>
        /// Start Event - Tạo GameRoom trong GameHub và gửi email notification cho tất cả users
        /// Email CHỈ được gửi SAU KHI room đã được tạo và verified thành công
        /// </summary>
        public async Task<StartEventResponseDto> StartEventAsync(Guid userId, StartEventRequestDto dto)
        {
            var eventEntity = await _eventRepo.GetByIdWithDetailsAsync(dto.EventId);
            if (eventEntity == null)
                throw new ArgumentException("Event không tồn tại");

            // Check owner
            if (eventEntity.CreatedBy != userId)
                throw new UnauthorizedAccessException("Chỉ người tạo Event mới có thể start");

            // Check status
            if (eventEntity.Status == "Active")
                throw new InvalidOperationException("Event đã được start rồi");

            if (eventEntity.Status == "Completed")
                throw new InvalidOperationException("Event đã kết thúc");

            // Validate QuizSet type
            if (eventEntity.QuizSet == null)
                throw new InvalidOperationException("QuizSet không tồn tại");

            if (eventEntity.QuizSet.QuizSetType != QuizSetTypeEnum.Event)
                throw new InvalidOperationException("QuizSet phải có QuizSetType là Event");

            // ✅ VALIDATION: QuizSet phải có ít nhất 1 câu hỏi trước khi start
            var quizzes = await _quizRepo.GetQuizzesByQuizSetIdAsync(eventEntity.QuizSetId);
            if (quizzes == null || !quizzes.Any())
                throw new InvalidOperationException("QuizSet của Event chưa có câu hỏi. Vui lòng thêm câu hỏi trước khi start Event.");

            // Check time
            var now = DateTime.UtcNow;
            if (now < eventEntity.StartDate)
                throw new InvalidOperationException("Chưa đến thời gian start Event");

            _logger.LogInformation($"🎮 Starting Event {eventEntity.Id}: Creating GameHub room...");

            // ✨ STEP 1: TẠO GAME ROOM TRONG GAMEHUB
            var createGameDto = new CreateGameDto
            {
                QuizSetId = eventEntity.QuizSetId,
                HostUserId = userId,
                HostUserName = dto.HostUserName,
                EventId = eventEntity.Id // ✨ Lưu EventId để sync điểm sau này
            };

            CreateGameResponseDto gameResponse;
            try
            {
                gameResponse = await _realtimeGameService.CreateGameAsync(createGameDto);
                _logger.LogInformation($"✅ GameHub room created successfully with PIN: {gameResponse.GamePin}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Failed to create GameHub room for Event {eventEntity.Id}");
                throw new InvalidOperationException("Không thể tạo game room. Vui lòng thử lại.", ex);
            }

            // ✨ STEP 2: VERIFY ROOM ĐÃ ĐƯỢC TẠO VÀ SẴN SÀNG
            try
            {
                var roomVerified = await VerifyGameRoomReadyAsync(gameResponse.GamePin);
                if (!roomVerified)
                {
                    _logger.LogError($"❌ Game room {gameResponse.GamePin} verification failed for Event {eventEntity.Id}");
                    throw new InvalidOperationException("Game room chưa sẵn sàng. Vui lòng thử lại.");
                }
                _logger.LogInformation($"✅ Game room {gameResponse.GamePin} verified and ready for participants");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Game room verification failed for Event {eventEntity.Id}");
                // Continue anyway - room might still work
            }

            // ✨ STEP 3: UPDATE EVENT STATUS
            eventEntity.Status = "Active";
            await _eventRepo.UpdateAsync(eventEntity);
            _logger.LogInformation($"✅ Event {eventEntity.Id} status updated to Active");

            // ✨ STEP 4: GỬI EMAIL NOTIFICATION - CHỈ SAU KHI ROOM ĐÃ SẴN SÀNG
            _logger.LogInformation($"📧 Initiating email notification for Event {eventEntity.Id} with GamePin {gameResponse.GamePin}");

            try
            {
                // Gửi email NGAY TRONG CÙNG SCOPE (không dùng Task.Run) để tránh dùng DbContext đã dispose
                await SendGamePinEmailToEventParticipantsAsync(
                    eventEntity,
                    gameResponse.GamePin,
                    gameResponse.GameSessionId);
            }
            catch (Exception ex)
            {
                // Log lỗi nhưng KHÔNG chặn việc start event
                // Email sending failed - silently continue
            }

            _logger.LogInformation($"🎉 Event {eventEntity.Name} (ID: {eventEntity.Id}) started successfully with GamePin: {gameResponse.GamePin}");

            return new StartEventResponseDto
            {
                EventId = eventEntity.Id,
                EventName = eventEntity.Name,
                GamePin = gameResponse.GamePin,
                GameSessionId = gameResponse.GameSessionId,
                StartedAt = gameResponse.CreatedAt,
                Status = "Active"
            };
        }

        public async Task<IEnumerable<EventParticipantResponseDto>> GetEventParticipantsAsync(Guid eventId)
        {
            var participants = await _eventParticipantRepo.GetByEventIdAsync(eventId);
            var result = new List<EventParticipantResponseDto>();

            foreach (var participant in participants)
            {
                result.Add(new EventParticipantResponseDto
                {
                    Id = participant.Id,
                    EventId = participant.EventId,
                    ParticipantId = participant.ParticipantId,
                    ParticipantName = participant.Participant?.FullName ??"Unknown",
                    Score = participant.Score,
                    Accuracy = participant.Accuracy,
                    Rank = participant.Rank,
                    JoinAt = participant.JoinAt,
                    FinishAt = participant.FinishAt
                });
            }

            return result;
        }

        /// <summary>
        /// Lấy Leaderboard của Event với ranking và badges
        /// Ưu tiên dùng EventParticipant.Score (đã được sync từ GameHub realtime game)
        /// Fallback sang QuizAttempt nếu EventParticipant.Score = 0
        /// </summary>
        public async Task<EventLeaderboardResponseDto> GetEventLeaderboardAsync(Guid eventId)
        {
            var eventEntity = await _eventRepo.GetByIdWithDetailsAsync(eventId);
            if (eventEntity == null)
                throw new ArgumentException("Event không tồn tại");

            var participants = await _eventParticipantRepo.GetByEventIdAsync(eventId);
            var participantList = participants.ToList();

            if (!participantList.Any())
            {
                return new EventLeaderboardResponseDto
                {
                    EventId = eventEntity.Id,
                    EventName = eventEntity.Name,
                    EventStatus = eventEntity.Status,
                    TotalParticipants = 0,
                    EventStartDate = eventEntity.StartDate,
                    EventEndDate = eventEntity.EndDate,
                    Rankings = new List<EventLeaderboardItemDto>(),
                    TopPlayer = null,
                    GeneratedAt = DateTime.UtcNow
                };
            }

            // Lấy tất cả attempts của QuizSet này một lần để tối ưu query (dùng làm fallback)
            var attempts = await _quizAttemptRepo.GetByQuizSetIdAsync(eventEntity.QuizSetId, includeDeleted: false);
            var validAttempts = attempts
                .Where(a => a.Status == "completed" && a.DeletedAt == null)
                .ToList();

            // Group attempts theo UserId để dễ xử lý
            var attemptsByUser = validAttempts
                .GroupBy(a => a.UserId)
                .ToDictionary(g => g.Key, g => g.ToList());

            var eventStartDate = eventEntity.StartDate.Date;
            var eventEndDate = eventEntity.EndDate.Date;
            var now = DateTime.UtcNow.Date;
            var effectiveEndDate = now < eventEndDate ? now : eventEndDate;

            // Tính điểm cho từng participant
            // ƯU TIÊN: Dùng EventParticipant.Score (đã được sync từ GameHub realtime game)
            // FALLBACK: Dùng QuizAttempt nếu EventParticipant.Score = 0
            var participantScores = new List<(EventParticipant Participant, long Score, double Accuracy, DateTime? FinishAt)>();

            foreach (var participant in participantList)
            {
                var user = participant.Participant;
                if (user == null) continue;

                long score = participant.Score; // Ưu tiên dùng điểm đã sync từ GameHub
                double accuracy = participant.Accuracy;
                DateTime? finishAt = participant.FinishAt;

                _logger.LogInformation($"📊 Participant {participant.ParticipantId} (Event {eventId}): Score={score}, Accuracy={accuracy:F2}%");

                // Nếu EventParticipant.Score = 0, fallback sang QuizAttempt
                if (score == 0)
                {
                    var participantJoinDate = participant.JoinAt.Date;
                    var startDate = participantJoinDate > eventStartDate ? participantJoinDate : eventStartDate;

                    // Lấy attempts của user này
                    if (attemptsByUser.TryGetValue(participant.ParticipantId, out var userAttempts))
                    {
                        // Lọc attempts chỉ tính những attempts được hoàn thành trong thời gian Event và sau khi join
                        var validUserAttempts = userAttempts
                            .Where(a =>
                            {
                                var attemptDate = (a.UpdatedAt ?? a.CreatedAt).Date;
                                return attemptDate >= eventStartDate
                                    && attemptDate <= effectiveEndDate
                                    && a.CreatedAt >= participant.JoinAt;
                            })
                            .ToList();

                        // Lấy attempt tốt nhất (score cao nhất, nếu bằng nhau thì lấy accuracy cao nhất)
                        var bestAttempt = validUserAttempts
                            .OrderByDescending(a => a.Score)
                            .ThenByDescending(a => a.Accuracy)
                            .ThenByDescending(a => a.UpdatedAt ?? a.CreatedAt)
                            .FirstOrDefault();

                        if (bestAttempt != null)
                        {
                            score = bestAttempt.Score;
                            accuracy = (double)bestAttempt.Accuracy;
                            finishAt = bestAttempt.UpdatedAt ?? bestAttempt.CreatedAt;
                            _logger.LogInformation($"📊 Participant {participant.ParticipantId}: Using QuizAttempt fallback - Score={score}, Accuracy={accuracy:F2}%");
                        }
                        else
                        {
                            _logger.LogWarning($"⚠️ Participant {participant.ParticipantId}: No QuizAttempt found for fallback, using Score=0");
                        }
                    }
                    else
                    {
                        _logger.LogWarning($"⚠️ Participant {participant.ParticipantId}: No QuizAttempts found for user, using Score=0");
                    }
                }

                _logger.LogInformation($"📊 Final score for Participant {participant.ParticipantId}: Score={score}, Accuracy={accuracy:F2}%");
                participantScores.Add((participant, score, accuracy, finishAt));
            }

            // Sắp xếp theo Score (cao → thấp), sau đó theo Accuracy, cuối cùng theo JoinAt
            var sortedScores = participantScores
                .OrderByDescending(x => x.Score)
                .ThenByDescending(x => x.Accuracy)
                .ThenBy(x => x.Participant.JoinAt)
                .ToList();

            // Tạo rankings với badges
            var rankings = new List<EventLeaderboardItemDto>();
            long currentRank = 1;

            foreach (var (participant, score, accuracy, finishAt) in sortedScores)
            {
                var user = participant.Participant;
                var isTopThree = currentRank <= 3;
                var badge = currentRank switch
                {
                    1 => "🥇",
                    2 => "🥈",
                    3 => "🥉",
                    _ => ""
                };

                // Map userName: ưu tiên FullName, nếu không có thì dùng Username
                var participantName = "Unknown";
                if (user != null)
                {
                    if (!string.IsNullOrWhiteSpace(user.FullName))
                    {
                        participantName = user.FullName;
                    }
                    else if (!string.IsNullOrWhiteSpace(user.Username))
                    {
                        participantName = user.Username;
                    }
                }

                rankings.Add(new EventLeaderboardItemDto
                {
                    Rank = currentRank,
                    ParticipantId = participant.ParticipantId,
                    ParticipantName = participantName,
                    AvatarUrl = user?.AvatarUrl,
                    Score = score,
                    Accuracy = accuracy,
                    JoinAt = participant.JoinAt,
                    FinishAt = finishAt,
                    IsTopThree = isTopThree,
                    Badge = badge
                });

                currentRank++;
            }

            // Lấy top player (rank 1)
            var topPlayer = rankings.FirstOrDefault();

            _logger.LogInformation($"✅ Event Leaderboard calculated for Event {eventId}: {rankings.Count} participants");

            return new EventLeaderboardResponseDto
            {
                EventId = eventEntity.Id,
                EventName = eventEntity.Name,
                EventStatus = eventEntity.Status,
                TotalParticipants = rankings.Count,
                EventStartDate = eventEntity.StartDate,
                EventEndDate = eventEntity.EndDate,
                Rankings = rankings,
                TopPlayer = topPlayer,
                GeneratedAt = DateTime.UtcNow
            };
        }

        public async Task<bool> JoinEventAsync(Guid eventId, Guid userId)
        {
            var eventEntity = await _eventRepo.GetByIdAsync(eventId);
            if (eventEntity == null)
                throw new ArgumentException("Event không tồn tại");

            // Check if event is in Upcoming state (cho phép đăng ký trước khi start)
            if (eventEntity.Status != "Upcoming")
            {
                if (eventEntity.Status == "Active")
                    throw new InvalidOperationException("Sự kiện đã bắt đầu. Bạn không thể tham gia sau khi sự kiện đã được bắt đầu.");
                else
                    throw new InvalidOperationException($"Sự kiện chưa bắt đầu hoặc đã kết thúc. Trạng thái hiện tại: {eventEntity.Status}");
            }

            // Check if already joined
            if (await _eventParticipantRepo.IsParticipantInEventAsync(eventId, userId))
                throw new InvalidOperationException("Bạn đã tham gia Event này rồi");

            // Check max participants
            var currentCount = await _eventParticipantRepo.CountParticipantsByEventIdAsync(eventId);
            if (currentCount >= eventEntity.MaxParticipants)
                throw new InvalidOperationException("Event đã đầy");

            var participant = new EventParticipant
            {
                Id = Guid.NewGuid(),
                EventId = eventId,
                ParticipantId = userId,
                Score = 0,
                Accuracy = 0,
                Rank = 0,
                JoinAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            };

            await _eventParticipantRepo.CreateAsync(participant);

            _logger.LogInformation($"✅ User {userId} joined Event {eventId}");

            return true;
        }

        public async Task<bool> IsUserJoinedAsync(Guid eventId, Guid userId)
        {
            return await _eventParticipantRepo.IsParticipantInEventAsync(eventId, userId);
        }

        /// <summary>
        /// Sync điểm từ GameSession (Redis) vào EventParticipant (Database)
        /// Được gọi khi game kết thúc để lưu điểm vào database
        /// </summary>
        public async Task SyncPlayerScoreAsync(Guid eventId, Guid userId, long score, double accuracy)
        {
            try
            {
                _logger.LogInformation($"🔄 Syncing score for Event {eventId}, User {userId}: Score={score}, Accuracy={accuracy:F2}%");

                // Tìm hoặc tạo EventParticipant
                var participant = await _eventParticipantRepo.GetByEventAndParticipantAsync(eventId, userId);

                if (participant == null)
                {
                    // Tạo mới nếu chưa có (trường hợp user join game nhưng chưa join event)
                    _logger.LogInformation($"Creating new EventParticipant for Event {eventId}, User {userId}");
                    
                    participant = new EventParticipant
                    {
                        Id = Guid.NewGuid(),
                        EventId = eventId,
                        ParticipantId = userId,
                        Score = score,
                        Accuracy = accuracy,
                        Rank = 0, // Sẽ được update bởi scheduler
                        JoinAt = DateTime.UtcNow,
                        FinishAt = DateTime.UtcNow,
                        CreatedAt = DateTime.UtcNow
                    };

                    await _eventParticipantRepo.CreateAsync(participant);
                    _logger.LogInformation($"✅ Created EventParticipant with Score={score}, Accuracy={accuracy:F2}%");
                }
                else
                {
                    // ✨ Nếu đã có FinishAt và điểm/accuracy không thay đổi, skip để tránh duplicate update
                    if (participant.FinishAt.HasValue 
                        && participant.Score == score 
                        && Math.Abs(participant.Accuracy - accuracy) < 0.01) // So sánh accuracy với tolerance
                    {
                        _logger.LogInformation($"⏭️ EventParticipant đã được sync trước đó (FinishAt: {participant.FinishAt}, Score: {participant.Score}, Accuracy: {participant.Accuracy:F2}%). Skip để tránh duplicate update.");
                        return;
                    }

                    // Update điểm nếu đã có (hoặc điểm/accuracy có thay đổi)
                    participant.Score = score;
                    participant.Accuracy = accuracy;
                    participant.FinishAt = DateTime.UtcNow;
                    participant.UpdatedAt = DateTime.UtcNow;

                    await _eventParticipantRepo.UpdateAsync(participant);
                    _logger.LogInformation($"✅ Updated EventParticipant with Score={score}, Accuracy={accuracy:F2}%");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Failed to sync player score for Event {eventId}, User {userId}");
                throw;
            }
        }

        /// <summary>
        /// Lưu lịch sử chơi Event vào QuizAttempt để user có thể xem lại
        /// AttemptType = "event"
        /// </summary>
        public async Task SaveEventGameHistoryAsync(
            Guid eventId, 
            Guid userId, 
            Guid quizSetId, 
            int totalQuestions, 
            int correctAnswers, 
            int wrongAnswers, 
            long score, 
            double accuracy, 
            int? timeSpent)
        {
            try
            {
                _logger.LogInformation($"📝 Saving Event game history for Event {eventId}, User {userId}: Score={score}, Accuracy={accuracy:F2}%");

                // ✨ Check duplicate: chỉ skip nếu đã có attempt gần đây (trong vòng 2 phút) với cùng QuizSetId
                // Điều này cho phép lưu nhiều attempt cho cùng QuizSetId trong các Event khác nhau
                // Nhưng vẫn tránh duplicate khi EndEvent được gọi nhiều lần
                var existingAttempts = await _quizAttemptRepo.GetByUserIdAsync(userId, includeDeleted: false);
                var recentAttempt = existingAttempts.FirstOrDefault(a => 
                    a.QuizSetId == quizSetId 
                    && a.AttemptType == "event" 
                    && a.Status == "completed"
                    && a.DeletedAt == null
                    && a.CreatedAt >= DateTime.UtcNow.AddMinutes(-2)); // Chỉ check trong vòng 2 phút gần đây

                if (recentAttempt != null)
                {
                    _logger.LogInformation($"⏭️ User {userId} đã có QuizAttempt gần đây cho QuizSet này (AttemptId: {recentAttempt.Id}, CreatedAt: {recentAttempt.CreatedAt}). Skip để tránh duplicate khi EndEvent được gọi nhiều lần.");
                    return; // Đã có attempt gần đây, skip để tránh duplicate
                }

                // Tính accuracy dạng decimal
                var accuracyDecimal = totalQuestions > 0 
                    ? (decimal)correctAnswers / totalQuestions 
                    : 0;

                // Tạo QuizAttempt với AttemptType = "event"
                var attempt = new Repository.Entities.QuizAttempt
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    QuizSetId = quizSetId,
                    AttemptType = "event",
                    TotalQuestions = totalQuestions,
                    CorrectAnswers = correctAnswers,
                    WrongAnswers = wrongAnswers,
                    Score = (int)score, // QuizAttempt.Score là int
                    Accuracy = accuracyDecimal,
                    IsCompleted = true,
                    TimeSpent = timeSpent,
                    Status = "completed",
                    OpponentId = null,
                    IsWinner = null,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                await _quizAttemptRepo.CreateAsync(attempt);
                _logger.LogInformation($"✅ Saved Event game history: AttemptId={attempt.Id}, EventId={eventId}, UserId={userId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Failed to save Event game history for Event {eventId}, User {userId}");
                // Không throw để không ảnh hưởng đến flow chính
            }
        }

        /// <summary>
        /// End Event - Cập nhật status thành "Ended" và tính toán rank cho participants
        /// </summary>
        public async Task<EndEventResponseDto> EndEventAsync(Guid userId, EndEventRequestDto dto)
        {
            var eventEntity = await _eventRepo.GetByIdWithDetailsAsync(dto.EventId);
            if (eventEntity == null)
                throw new ArgumentException("Event không tồn tại");

            // Check owner
            if (eventEntity.CreatedBy != userId)
                throw new UnauthorizedAccessException("Chỉ người tạo Event mới có thể end");

            // Check status: cho phép Active hoặc Cancelled (để hỗ trợ trường hợp bị cancel trước đó)
            if (eventEntity.Status != "Active" && eventEntity.Status != "Cancelled")
                throw new InvalidOperationException($"Không thể end Event với status '{eventEntity.Status}'. Chỉ có thể end Event đang Active hoặc Cancelled.");

            // Bắt buộc có GamePin để lấy kết quả từ Redis
            if (string.IsNullOrWhiteSpace(dto.GamePin))
                throw new ArgumentException("GamePin không được để trống khi kết thúc Event.");

            // Lấy final result và session từ Redis để sync điểm (không phụ thuộc Hub)
            var finalResult = await _realtimeGameService.GetFinalResultAsync(dto.GamePin);
            var session = await _realtimeGameService.GetGameSessionAsync(dto.GamePin);

            // Nếu finalResult null (có thể game chưa set Completed) thì fallback dựng từ session
            if (finalResult == null)
            {
                if (session == null)
                    throw new InvalidOperationException("Không thể lấy kết quả cuối cùng từ Redis. Vui lòng kiểm tra GamePin hoặc trạng thái game.");

                finalResult = BuildFinalResultFromSession(dto.GamePin, session);
                _logger.LogWarning($"⚠️ FinalResult null cho game {dto.GamePin}, dùng dữ liệu session để sync điểm.");
            }
            else if (session == null)
            {
                // finalResult có nhưng session bị cleanup? cố gắng tiếp tục với finalResult
                _logger.LogWarning($"⚠️ Session không tồn tại nhưng có FinalResult cho game {dto.GamePin}, tiếp tục sync với FinalResult.");
            }

            // Đảm bảo EventId khớp
            if (session != null && (!session.EventId.HasValue || session.EventId.Value != dto.EventId))
                throw new InvalidOperationException("Game session không thuộc Event này hoặc thiếu EventId.");

            _logger.LogInformation($"🏁 Ending Event {eventEntity.Id}: {eventEntity.Name}");

            // Step 0: Sync điểm từ session.Players hoặc finalResult vào EventParticipant + lưu history
            // ✨ Sync trực tiếp từ session.Players để đảm bảo không bỏ sót player nào
            int syncedCount = 0;
            int skippedCount = 0;

            // Ưu tiên dùng session.Players (có UserId), fallback về finalResult.FinalRankings nếu session null
            List<PlayerInfo> playersToSync = new List<PlayerInfo>();
            
            if (session != null && session.Players != null && session.Players.Count > 0)
            {
                // Ưu tiên: sync từ session.Players (có đầy đủ thông tin UserId)
                playersToSync = session.Players;
                _logger.LogInformation($"📊 Bắt đầu sync điểm cho {playersToSync.Count} player(s) từ session");
            }
            else if (finalResult != null && finalResult.FinalRankings != null && finalResult.FinalRankings.Count > 0)
            {
                // Fallback: nếu session null, dùng finalResult nhưng cần match với players từ session (nếu có)
                _logger.LogWarning($"⚠️ Session null hoặc không có players, dùng finalResult.FinalRankings. Sẽ cần match với UserId từ database.");
                
                // Nếu session null, không thể lấy UserId từ session, cần skip
                if (session == null)
                {
                    _logger.LogError($"❌ Không thể sync điểm vì session null và không có cách nào lấy UserId. Cần session để lấy UserId.");
                    throw new InvalidOperationException("Không thể sync điểm vì session đã bị cleanup. Vui lòng đảm bảo EndEvent được gọi trước khi session bị cleanup.");
                }
            }
            else
            {
                _logger.LogWarning($"⚠️ Không có players nào để sync điểm. Session players: {session?.Players?.Count ?? 0}, FinalRankings: {finalResult?.FinalRankings?.Count ?? 0}");
            }

            // ✨ Sync trực tiếp từ session.Players thay vì từ FinalRankings để đảm bảo không bỏ sót
            foreach (var player in playersToSync)
            {
                try
                {
                    // Bỏ qua nếu không có UserId
                    if (!player.UserId.HasValue)
                    {
                        skippedCount++;
                        _logger.LogWarning($"⚠️ Bỏ qua lưu history/score cho player '{player.PlayerName}' vì không có UserId.");
                        continue;
                    }

                    // ✨ Check xem đã sync chưa (tránh duplicate sync)
                    var existingParticipant = await _eventParticipantRepo.GetByEventAndParticipantAsync(eventEntity.Id, player.UserId.Value);
                    if (existingParticipant != null && existingParticipant.FinishAt.HasValue)
                    {
                        // Đã sync rồi (có FinishAt) - skip để tránh duplicate
                        _logger.LogInformation($"⏭️ Player '{player.PlayerName}' (UserId: {player.UserId}) đã được sync trước đó (FinishAt: {existingParticipant.FinishAt}). Skip để tránh duplicate.");
                        skippedCount++;
                        continue;
                    }

                    // ✨ Dùng session.Questions.Count thay vì finalResult.TotalQuestions để đảm bảo đúng số câu hỏi thực tế
                    var totalQuestions = session.Questions?.Count ?? 0;
                    if (totalQuestions == 0 && finalResult != null)
                    {
                        // Fallback nếu session không có questions
                        totalQuestions = finalResult.TotalQuestions;
                    }

                    // Tính toán accuracy và wrong answers
                    var accuracy = totalQuestions > 0
                        ? (double)player.CorrectAnswers / totalQuestions * 100
                        : 0;
                    var wrongAnswers = player.TotalAnswered - player.CorrectAnswers;

                    _logger.LogInformation($"🔄 Syncing score for player '{player.PlayerName}' (UserId: {player.UserId}): Score={player.Score}, Correct={player.CorrectAnswers}/{totalQuestions}, TotalAnswered={player.TotalAnswered}, Accuracy={accuracy:F2}%");

                    // Sync điểm vào EventParticipant
                    await SyncPlayerScoreAsync(
                        eventEntity.Id,
                        player.UserId.Value,
                        player.Score,
                        accuracy);

                    // ✨ Lưu lịch sử chơi Event vào QuizAttempt (mỗi Event sẽ tạo một attempt riêng)
                    // Logic check duplicate đã được xử lý trong SaveEventGameHistoryAsync
                    // ✨ Dùng totalQuestions từ session thay vì finalResult để đảm bảo đúng
                    await SaveEventGameHistoryAsync(
                        eventEntity.Id,
                        player.UserId.Value,
                        session.QuizSetId,
                        totalQuestions,
                        player.CorrectAnswers,
                        wrongAnswers,
                        player.Score,
                        accuracy,
                        timeSpent: null);

                    syncedCount++;
                    _logger.LogInformation($"✅ Đã sync thành công cho player '{player.PlayerName}' (UserId: {player.UserId})");
                }
                catch (Exception ex)
                {
                    skippedCount++;
                    _logger.LogError(ex, $"❌ Lỗi khi sync điểm cho player '{player.PlayerName}' (UserId: {player.UserId?.ToString() ?? "N/A"}). Tiếp tục với player tiếp theo.");
                    // Tiếp tục với player tiếp theo, không throw để không dừng toàn bộ quá trình
                }
            }

            _logger.LogInformation($"📊 EndEvent sync completed. Synced={syncedCount}, Skipped={skippedCount}, TotalPlayers={playersToSync.Count}");

            // Step 1: Update Event status
            eventEntity.Status = "Ended";
            eventEntity.UpdatedAt = DateTime.UtcNow;
            await _eventRepo.UpdateAsync(eventEntity);
            _logger.LogInformation($"✅ Event {eventEntity.Id} status updated to 'Ended'");

            // Step 2: Update participant ranks dựa trên score
            await UpdateParticipantRanksAsync(eventEntity.Id);

            // Step 3: Count participants
            var totalParticipants = await _eventParticipantRepo.CountParticipantsByEventIdAsync(eventEntity.Id);

            // Step 4: Cleanup game session trong Redis (sau khi đã sync)
            try
            {
                await _realtimeGameService.CleanupGameAsync(dto.GamePin);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"⚠️ Cleanup game session failed for GamePin {dto.GamePin} (có thể đã bị dọn trước đó)");
            }

            _logger.LogInformation($"🎉 Event {eventEntity.Id} ({eventEntity.Name}) ended successfully with {totalParticipants} participants");

            return new EndEventResponseDto
            {
                EventId = eventEntity.Id,
                EventName = eventEntity.Name,
                Status = "Ended",
                EndedAt = DateTime.UtcNow,
                TotalParticipants = (int)totalParticipants
            };
        }

        /// <summary>
        /// Update rank cho tất cả participants của Event
        /// Rank dựa trên Score (cao → thấp), sau đó Accuracy
        /// </summary>
        private async Task UpdateParticipantRanksAsync(Guid eventId)
        {
            try
            {
                // Lấy tất cả participants và sort
                var participants = await _eventParticipantRepo.GetByEventIdAsync(eventId);
                var sortedParticipants = participants
                    .OrderByDescending(p => p.Score)
                    .ThenByDescending(p => p.Accuracy)
                    .ThenBy(p => p.JoinAt)
                    .ToList();

                if (!sortedParticipants.Any())
                {
                    _logger.LogDebug($"No participants found for Event {eventId}");
                    return;
                }

                _logger.LogInformation($"📊 Updating ranks for {sortedParticipants.Count} participant(s)");

                // Update rank cho từng participant
                long currentRank = 1;
                foreach (var participant in sortedParticipants)
                {
                    participant.Rank = currentRank;
                    participant.UpdatedAt = DateTime.UtcNow;
                    
                    // Set FinishAt nếu chưa có
                    if (!participant.FinishAt.HasValue)
                    {
                        participant.FinishAt = DateTime.UtcNow;
                    }

                    await _eventParticipantRepo.UpdateAsync(participant);
                    
                    _logger.LogDebug($"Updated Rank {currentRank} for Participant {participant.ParticipantId}");
                    currentRank++;
                }

                _logger.LogInformation($"✅ Successfully updated ranks for {sortedParticipants.Count} participant(s)");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Failed to update participant ranks for Event {eventId}");
                throw;
            }
        }

        /// <summary>
        /// Cập nhật status của Event (Ended, Cancelled, etc.)
        /// </summary>
        public async Task<bool> UpdateEventStatusAsync(Guid eventId, string status)
        {
            try
            {
                _logger.LogInformation($"🔄 Updating Event {eventId} status to {status}");

                var eventEntity = await _eventRepo.GetByIdAsync(eventId);
                if (eventEntity == null)
                {
                    _logger.LogWarning($"⚠️ Event {eventId} not found");
                    return false;
                }

                // Validate status
                var validStatuses = new[] { "Upcoming", "Active", "Ended", "Cancelled" };
                if (!validStatuses.Contains(status))
                {
                    _logger.LogWarning($"⚠️ Invalid status: {status}. Valid statuses: {string.Join(", ", validStatuses)}");
                    throw new ArgumentException($"Status không hợp lệ: {status}");
                }

                // Cho phép update từ Active sang Ended hoặc Cancelled
                if (eventEntity.Status == "Active" && (status == "Ended" || status == "Cancelled"))
                {
                    eventEntity.Status = status;
                    eventEntity.UpdatedAt = DateTime.UtcNow;
                    await _eventRepo.UpdateAsync(eventEntity);
                    _logger.LogInformation($"✅ Event {eventId} status updated from Active to {status}");
                    return true;
                }
                // Cho phép update từ Cancelled sang Ended (khi game đã hoàn thành và có kết quả)
                else if (eventEntity.Status == "Cancelled" && status == "Ended")
                {
                    eventEntity.Status = status;
                    eventEntity.UpdatedAt = DateTime.UtcNow;
                    await _eventRepo.UpdateAsync(eventEntity);
                    _logger.LogInformation($"✅ Event {eventId} status updated from Cancelled to Ended (game completed with results)");
                    return true;
                }
                else if (eventEntity.Status != status)
                {
                    _logger.LogWarning($"⚠️ Cannot update Event {eventId} status from {eventEntity.Status} to {status}");
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Failed to update Event {eventId} status to {status}");
                throw;
            }
        }

        private async Task<EventResponseDto> MapToResponseDto(Event entity)
        {
            var currentParticipants = await _eventParticipantRepo.CountParticipantsByEventIdAsync(entity.Id);

            return new EventResponseDto
            {
                Id = entity.Id,
                QuizSetId = entity.QuizSetId,
                QuizSetTitle = entity.QuizSet?.Title ?? "Unknown",
                Name = entity.Name,
                Description = entity.Description,
                StartDate = entity.StartDate,
                EndDate = entity.EndDate,
                MaxParticipants = entity.MaxParticipants,
                CurrentParticipants = currentParticipants,
                Status = entity.Status,
                CreatedBy = entity.CreatedBy,
                CreatorName = entity.Creator?.FullName ?? "Unknown",
                CreatedAt = entity.CreatedAt,
                UpdatedAt = entity.UpdatedAt
            };
        }

        /// <summary>
        /// Verify rằng Game Room đã được tạo và sẵn sàng cho users join
        /// </summary>
        private async Task<bool> VerifyGameRoomReadyAsync(string gamePin)
        {
            try
            {
                _logger.LogInformation($"🔍 Verifying game room with PIN: {gamePin}");
                
                // Get game session từ RealtimeGameService để verify
                var session = await _realtimeGameService.GetGameSessionAsync(gamePin);
                
                if (session == null)
                {
                    _logger.LogWarning($"⚠️ Game session not found for PIN: {gamePin}");
                    return false;
                }

                // Verify room status là Lobby (ready for players to join)
                if (session.Status != GameStatus.Lobby)
                {
                    _logger.LogWarning($"⚠️ Game room {gamePin} has status: {session.Status}, expected: Lobby");
                    return false;
                }

                // Verify có questions
                if (session.Questions == null || !session.Questions.Any())
                {
                    _logger.LogWarning($"⚠️ Game room {gamePin} has no questions");
                    return false;
                }

                _logger.LogInformation($"✅ Game room {gamePin} verified: Status={session.Status}, Questions={session.Questions.Count}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Error verifying game room {gamePin}");
                return false;
            }
        }

        /// <summary>
        /// Gửi email với GamePin cho TẤT CẢ users đã ĐĂNG KÝ tham gia Event (EventParticipants)
        /// Method này CHỈ được gọi SAU KHI game room đã được tạo và verified thành công
        /// </summary>
        private async Task SendGamePinEmailToEventParticipantsAsync(
            Event eventEntity,
            string gamePin,
            Guid gameSessionId)
        {
            var startTime = DateTime.UtcNow;
            _logger.LogInformation($"📧 Starting email sending process for Event {eventEntity.Id}, GamePin: {gamePin}");

            try
            {
                // ✅ STEP 1: LẤY DANH SÁCH USERS ĐÃ ĐĂNG KÝ THAM GIA EVENT
                _logger.LogInformation($"📋 Fetching event participants from database for Event {eventEntity.Id}...");

                var participants = await _eventParticipantRepo.GetByEventIdAsync(eventEntity.Id);
                var participantList = participants.ToList();

                if (!participantList.Any())
                {
                    _logger.LogWarning($"⚠️ Không tìm thấy người tham gia nào cho Event {eventEntity.Id}. Bỏ qua việc gửi email GamePin.");
                    _logger.LogWarning($"⚠️ LƯU Ý: User cần đăng ký tham gia Event (POST /api/event/{{id}}/join) TRƯỚC KHI start event để nhận email!");
                    return;
                }

                _logger.LogInformation($"📋 Tìm thấy {participantList.Count} người đã đăng ký tham gia Event {eventEntity.Id}");

                // Lấy User và Account tương ứng
                var accounts = new List<Account>();
                foreach (var participant in participantList)
                {
                    try
                    {
                        var user = await _userRepo.GetByIdAsync(participant.ParticipantId);
                        if (user == null)
                        {
                            _logger.LogWarning($"⚠️ User {participant.ParticipantId} not found for Event {eventEntity.Id}");
                            continue;
                        }

                        var account = await _accountRepo.GetByIdAsync(user.AccountId);
                        if (account == null)
                        {
                            _logger.LogWarning($"⚠️ Account for User {user.Id} (AccountId={user.AccountId}) not found. Skipping.");
                            continue;
                        }

                        // Chỉ gửi cho account active, email verified và có email hợp lệ
                        if (!account.IsActive || !account.IsEmailVerified || string.IsNullOrWhiteSpace(account.Email))
                        {
                            _logger.LogInformation($"ℹ️ Skipping Account {account.Id} (Active={account.IsActive}, Verified={account.IsEmailVerified}, Email='{account.Email}')");
                            continue;
                        }

                        accounts.Add(account);
                    }
                    catch (Exception ex)
                    {
                        // Error resolving Account - silently continue
                    }
                }

                if (!accounts.Any())
                {
                    _logger.LogWarning($"⚠️ Không tìm thấy tài khoản hợp lệ nào để gửi thông báo cho Event {eventEntity.Id}. Bỏ qua việc gửi email GamePin.");
                    _logger.LogWarning($"⚠️ LƯU Ý: Tài khoản cần có IsActive=true, IsEmailVerified=true và Email hợp lệ!");
                    return;
                }

                _logger.LogInformation($"✅ Found {accounts.Count} registered participants with valid email to notify for Event {eventEntity.Id}");

                // ✅ STEP 2: PREPARE EMAIL CONTENT
                var emailConfig = PrepareEmailConfiguration(eventEntity, gamePin);
                _logger.LogInformation($"✅ Email content prepared");

                // ✅ STEP 3: GỬI EMAILS THEO BATCH
                await SendEmailsInBatchesAsync(accounts, emailConfig, eventEntity.Id);

                var duration = (DateTime.UtcNow - startTime).TotalSeconds;
                _logger.LogInformation($"🎉 Successfully sent GamePin emails to {accounts.Count} registered participants in {duration:F2}s");
            }
            catch (Exception ex)
            {
                var duration = (DateTime.UtcNow - startTime).TotalSeconds;
                // Email sending failed - silently continue
                throw;
            }
        }

        /// <summary>
        /// Prepare email configuration với GamePin và event details
        /// </summary>
        private EmailConfiguration PrepareEmailConfiguration(Event eventEntity, string gamePin)
        {
            var fromEmail = _configuration["MailerSend:FromEmail"] ?? "no-reply@quizuplearn.com";
            var fromName = _configuration["MailerSend:FromName"] ?? "QuizUpLearn";

            // Validate configuration
            if (string.IsNullOrWhiteSpace(fromEmail))
            {
                _logger.LogWarning("⚠️ MailerSend:FromEmail is not configured, using default");
                fromEmail = "no-reply@quizuplearn.com";
            }

            // MailerSend:ApiKey validation - silently continue if not configured

            var htmlBody = CreateGamePinEmailTemplate(eventEntity, gamePin);
            var textBody = CreatePlainTextEmail(eventEntity, gamePin);

            _logger.LogInformation($"📧 Email config prepared: From={fromEmail}, Subject length={$"🎉 Event: {eventEntity.Name} - GamePin: {gamePin}".Length}, Html length={htmlBody.Length}, Text length={textBody.Length}");

            return new EmailConfiguration
            {
                FromEmail = fromEmail,
                FromName = fromName,
                Subject = $"🎉 Event: {eventEntity.Name} - GamePin: {gamePin}",
                HtmlBody = htmlBody,
                TextBody = textBody
            };
        }

        /// <summary>
        /// Gửi emails theo batch với retry logic và rate limiting
        /// </summary>
        private async Task SendEmailsInBatchesAsync(
            List<Account> accounts, 
            EmailConfiguration config, 
            Guid eventId)
        {
            const int BATCH_SIZE = 50; // MailerSend limit per request
            const int DELAY_MS = 150; // Delay between batches
            const int MAX_RETRIES = 3;

            var batches = accounts
                .Select((account, index) => new { account, index })
                .GroupBy(x => x.index / BATCH_SIZE)
                .Select(g => g.Select(x => x.account).ToList())
                .ToList();

            _logger.LogInformation($"📦 Split into {batches.Count} batches (max {BATCH_SIZE} recipients/batch)");

            int successCount = 0;
            int failCount = 0;

            for (int i = 0; i < batches.Count; i++)
            {
                var batch = batches[i];
                var batchNum = i + 1;

                try
                {
                    await SendSingleBatchWithRetryAsync(batch, config, batchNum, MAX_RETRIES);
                    successCount += batch.Count;
                    _logger.LogInformation($"✅ Batch {batchNum}/{batches.Count} sent ({batch.Count} recipients)");

                    // Rate limiting delay (except last batch)
                    if (i < batches.Count - 1)
                    {
                        await Task.Delay(DELAY_MS);
                    }
                }
                catch (Exception ex)
                {
                    failCount += batch.Count;
                    // Batch failed after retries - silently continue
                }
            }

            _logger.LogInformation($"📊 Email batch summary: {successCount} sent, {failCount} failed, {batches.Count} total batches");
        }

        /// <summary>
        /// Gửi một batch với retry logic
        /// </summary>
        private async Task SendSingleBatchWithRetryAsync(
            List<Account> batch,
            EmailConfiguration config,
            int batchNumber,
            int maxRetries)
        {
            Exception? lastError = null;

            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    var email = new MailerSendEmail
                    {
                        From = new MailerSendRecipient 
                        { 
                            Name = config.FromName, 
                            Email = config.FromEmail 
                        },
                        Subject = config.Subject,
                        Html = config.HtmlBody,
                        Text = config.TextBody
                    };

                    // Add recipients
                    foreach (var account in batch)
                    {
                        try
                        {
                            var displayName = account.User?.FullName 
                                ?? account.Email?.Split('@').FirstOrDefault() 
                                ?? "User";
                            
                            if (string.IsNullOrWhiteSpace(account.Email))
                            {
                                _logger.LogWarning($"⚠️ Skipping account {account.Id} - Email is null or empty");
                                continue;
                            }
                            
                            email.To.Add(new MailerSendRecipient
                            {
                                Name = displayName,
                                Email = account.Email
                            });
                        }
                        catch (Exception ex)
                        {
                            // Error adding recipient - continue với account khác
                        }
                    }

                    if (!email.To.Any())
                    {
                        _logger.LogWarning($"⚠️ Batch {batchNumber} has no valid recipients after processing");
                        return; // Skip batch nếu không có recipient nào
                    }

                    _logger.LogInformation($"📧 Sending batch {batchNumber} with {email.To.Count} recipients");
                    await _mailerSendService.SendEmailAsync(email);
                    _logger.LogInformation($"✅ Batch {batchNumber} sent successfully");
                    return; // Success!
                }
                catch (Exception ex)
                {
                    lastError = ex;
                    
                    if (attempt < maxRetries)
                    {
                        var delay = attempt * 1000; // Exponential backoff
                        _logger.LogWarning($"⚠️ Batch {batchNumber} attempt {attempt} failed, retrying in {delay}ms... Error: {ex.Message}");
                        await Task.Delay(delay);
                    }
                }
            }

            // All retries exhausted
            throw new InvalidOperationException(
                $"Không thể gửi batch {batchNumber} sau {maxRetries} lần thử", 
                lastError);
        }

        /// <summary>
        /// Tạo plain text version của email
        /// </summary>
        private string CreatePlainTextEmail(Event eventEntity, string gamePin)
        {
            var startDate = eventEntity.StartDate.ToString("dd/MM/yyyy HH:mm");
            var endDate = eventEntity.EndDate.ToString("dd/MM/yyyy HH:mm");

            return $@"
🎉 EVENT MỚI ĐÃ BẮT ĐẦU!

{eventEntity.Name}
{eventEntity.Description}

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
GAME PIN: {gamePin}
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

📅 Thời gian bắt đầu: {startDate}
⏰ Thời gian kết thúc: {endDate}
👥 Số người tối đa: {eventEntity.MaxParticipants}
📚 Quiz Set: {eventEntity.QuizSet?.Title ?? "Event Quiz"}

💡 CÁCH THAM GIA:
1. Mở ứng dụng QuizUpLearn
2. Nhập Game PIN: {gamePin}
3. Bắt đầu chơi ngay!

Chúc bạn may mắn! 🍀

---
© 2025 QuizUpLearn
";
        }

        /// <summary>
        /// Helper class để lưu email configuration
        /// </summary>
        private class EmailConfiguration
        {
            public string FromEmail { get; set; } = string.Empty;
            public string FromName { get; set; } = string.Empty;
            public string Subject { get; set; } = string.Empty;
            public string HtmlBody { get; set; } = string.Empty;
            public string TextBody { get; set; } = string.Empty;
        }

        /// <summary>
        /// Tạo HTML email template với GamePin nổi bật
        /// Template được optimize cho email clients và mobile devices
        /// </summary>
        private string CreateGamePinEmailTemplate(Event eventEntity, string gamePin)
        {
            var startDate = eventEntity.StartDate.ToString("dd/MM/yyyy HH:mm");
            var endDate = eventEntity.EndDate.ToString("dd/MM/yyyy HH:mm");

            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <style>
        body {{
            font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif;
            line-height: 1.6;
            color: #333;
            margin: 0;
            padding: 0;
            background-color: #f4f4f4;
        }}
        .container {{
            max-width: 600px;
            margin: 20px auto;
            background: #ffffff;
            border-radius: 12px;
            overflow: hidden;
            box-shadow: 0 4px 6px rgba(0, 0, 0, 0.1);
        }}
        .header {{
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            color: white;
            padding: 40px 30px;
            text-align: center;
        }}
        .header h1 {{
            margin: 0;
            font-size: 28px;
            font-weight: 700;
        }}
        .content {{
            padding: 40px 30px;
        }}
        .event-title {{
            font-size: 24px;
            font-weight: 700;
            color: #667eea;
            margin: 0 0 20px 0;
        }}
        .event-description {{
            color: #666;
            margin: 0 0 30px 0;
            line-height: 1.8;
        }}
        .game-pin-box {{
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            color: white;
            padding: 30px;
            border-radius: 8px;
            text-align: center;
            margin: 30px 0;
        }}
        .game-pin-label {{
            font-size: 14px;
            text-transform: uppercase;
            letter-spacing: 1px;
            opacity: 0.9;
            margin: 0 0 10px 0;
        }}
        .game-pin {{
            font-size: 48px;
            font-weight: 900;
            letter-spacing: 8px;
            margin: 0;
        }}
        .info-grid {{
            display: grid;
            grid-template-columns: 1fr 1fr;
            gap: 20px;
            margin: 30px 0;
        }}
        .info-item {{
            background: #f8f9fa;
            padding: 20px;
            border-radius: 8px;
            border-left: 4px solid #667eea;
        }}
        .info-label {{
            font-size: 12px;
            text-transform: uppercase;
            color: #888;
            margin: 0 0 5px 0;
            font-weight: 600;
        }}
        .info-value {{
            font-size: 16px;
            color: #333;
            font-weight: 600;
            margin: 0;
        }}
        .cta-button {{
            display: inline-block;
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            color: white;
            padding: 16px 40px;
            text-decoration: none;
            border-radius: 8px;
            font-weight: 700;
            font-size: 16px;
            text-align: center;
            margin: 20px 0;
        }}
        .footer {{
            background: #f8f9fa;
            padding: 30px;
            text-align: center;
            color: #888;
            font-size: 14px;
        }}
        @media only screen and (max-width: 600px) {{
            .info-grid {{
                grid-template-columns: 1fr;
            }}
            .game-pin {{
                font-size: 36px;
            }}
        }}
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1>🎉 Event Mới Đã Bắt Đầu!</h1>
        </div>
        
        <div class=""content"">
            <h2 class=""event-title"">{eventEntity.Name}</h2>
            <p class=""event-description"">{eventEntity.Description}</p>
            
            <div class=""game-pin-box"">
                <p class=""game-pin-label"">Game PIN để tham gia</p>
                <h1 class=""game-pin"">{gamePin}</h1>
            </div>
            
            <div class=""info-grid"">
                <div class=""info-item"">
                    <p class=""info-label"">📅 Bắt đầu</p>
                    <p class=""info-value"">{startDate}</p>
                </div>
                <div class=""info-item"">
                    <p class=""info-label"">⏰ Kết thúc</p>
                    <p class=""info-value"">{endDate}</p>
                </div>
                <div class=""info-item"">
                    <p class=""info-label"">👥 Giới hạn</p>
                    <p class=""info-value"">{eventEntity.MaxParticipants} người</p>
                </div>
                <div class=""info-item"">
                    <p class=""info-label"">📚 Quiz Set</p>
                    <p class=""info-value"">{eventEntity.QuizSet?.Title ?? "Quiz Event"}</p>
                </div>
            </div>
            
            <center>
                <a href=""https://quiz-up-learn.vercel.app/event/{eventEntity.Id}"" class=""cta-button"">
                    Tham Gia Ngay 🚀
                </a>
            </center>
            
            <p style=""margin-top: 30px; color: #666; font-size: 14px; text-align: center;"">
                💡 <strong>Cách tham gia:</strong><br>
                1. Truy cập ứng dụng QuizUpLearn<br>
                2. Nhập Game PIN: <strong>{gamePin}</strong><br>
                3. Bắt đầu chơi và tranh tài cùng mọi người!
            </p>
        </div>
        
        <div class=""footer"">
            <p style=""margin: 0 0 10px 0;"">
                © 2025 QuizUpLearn. All rights reserved.
            </p>
            <p style=""margin: 0; font-size: 12px;"">
                Bạn nhận được email này vì bạn là thành viên của QuizUpLearn.<br>
            </p>
        </div>
    </div>
</body>
</html>";
        }
    }
}

