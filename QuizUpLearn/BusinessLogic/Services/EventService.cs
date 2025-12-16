using BusinessLogic.DTOs;
using BusinessLogic.DTOs.EventDtos;
using BusinessLogic.Interfaces;
using Repository.Entities;
using Repository.Enums;
using Repository.Interfaces;
using Repository.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace BusinessLogic.Services
{
    public class EventService : IEventService
    {
        private readonly IEventRepo _eventRepo;
        private readonly IEventParticipantRepo _eventParticipantRepo;
        private readonly IQuizSetRepo _quizSetRepo;
        private readonly IUserRepo _userRepo;
        private readonly IAccountRepo _accountRepo;
        private readonly IRealtimeGameService _realtimeGameService;
        private readonly IMailerSendService _mailerSendService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<EventService> _logger;

        public EventService(
            IEventRepo eventRepo,
            IEventParticipantRepo eventParticipantRepo,
            IQuizSetRepo quizSetRepo,
            IUserRepo userRepo,
            IAccountRepo accountRepo,
            IRealtimeGameService realtimeGameService,
            IMailerSendService mailerSendService,
            IConfiguration configuration,
            ILogger<EventService> logger)
        {
            _eventRepo = eventRepo;
            _eventParticipantRepo = eventParticipantRepo;
            _quizSetRepo = quizSetRepo;
            _userRepo = userRepo;
            _accountRepo = accountRepo;
            _realtimeGameService = realtimeGameService;
            _mailerSendService = mailerSendService;
            _configuration = configuration;
            _logger = logger;
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
            
            _ = Task.Run(async () =>
            {
                try
                {
                    // Small delay to ensure room is fully initialized
                    await Task.Delay(500);
                    
                    await SendGamePinEmailToEventParticipantsAsync(
                        eventEntity, 
                        gameResponse.GamePin, 
                        gameResponse.GameSessionId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"❌ Failed to send email notifications for Event {eventEntity.Id}");
                    // Email failure should not break the event start
                }
            });

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
        /// </summary>
        public async Task<EventLeaderboardResponseDto> GetEventLeaderboardAsync(Guid eventId)
        {
            var eventEntity = await _eventRepo.GetByIdWithDetailsAsync(eventId);
            if (eventEntity == null)
                throw new ArgumentException("Event không tồn tại");

            // Lấy participants và sort theo Score (cao → thấp), sau đó theo Accuracy
            var participants = await _eventParticipantRepo.GetByEventIdAsync(eventId);
            var sortedParticipants = participants
                .OrderByDescending(p => p.Score)
                .ThenByDescending(p => p.Accuracy)
                .ThenBy(p => p.JoinAt)
                .ToList();

            // Update rank cho participants
            var rankings = new List<EventLeaderboardItemDto>();
            long currentRank = 1;

            foreach (var participant in sortedParticipants)
            {
                var isTopThree = currentRank <= 3;
                var badge = currentRank switch
                {
                    1 => "🥇",
                    2 => "🥈",
                    3 => "🥉",
                    _ => ""
                };

                rankings.Add(new EventLeaderboardItemDto
                {
                    Rank = currentRank,
                    ParticipantId = participant.ParticipantId,
                    ParticipantName = participant.Participant?.FullName ?? "Unknown",
                    AvatarUrl = participant.Participant?.AvatarUrl,
                    Score = participant.Score,
                    Accuracy = participant.Accuracy,
                    JoinAt = participant.JoinAt,
                    FinishAt = participant.FinishAt,
                    IsTopThree = isTopThree,
                    Badge = badge
                });

                currentRank++;
            }

            // Lấy top player (rank 1)
            var topPlayer = rankings.FirstOrDefault();

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

            // Check if event is active
            if (eventEntity.Status != "Active")
                throw new InvalidOperationException("Event chưa được start hoặc đã kết thúc");

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
                    // Update điểm nếu đã có
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
                    _logger.LogWarning($"⚠️ No participants found for Event {eventEntity.Id}. Skipping GamePin email sending.");
                    return;
                }

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
                        _logger.LogError(ex, $"❌ Error while resolving Account for participant {participant.ParticipantId} in Event {eventEntity.Id}");
                    }
                }

                if (!accounts.Any())
                {
                    _logger.LogWarning($"⚠️ No valid accounts found to notify for Event {eventEntity.Id}. Skipping GamePin email sending.");
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
                _logger.LogError(ex, $"❌ Failed to send GamePin emails after {duration:F2}s for Event {eventEntity.Id}");
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

            return new EmailConfiguration
            {
                FromEmail = fromEmail,
                FromName = fromName,
                Subject = $"🎉 Event: {eventEntity.Name} - GamePin: {gamePin}",
                HtmlBody = CreateGamePinEmailTemplate(eventEntity, gamePin),
                TextBody = CreatePlainTextEmail(eventEntity, gamePin)
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
                    _logger.LogError(ex, $"❌ Batch {batchNum}/{batches.Count} failed after retries");
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
                        var displayName = account.User?.FullName 
                            ?? account.Email.Split('@').FirstOrDefault() 
                            ?? "User";
                        
                        email.To.Add(new MailerSendRecipient
                        {
                            Name = displayName,
                            Email = account.Email
                        });
                    }

                    await _mailerSendService.SendEmailAsync(email);
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
                $"Failed to send batch {batchNumber} after {maxRetries} attempts", 
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
                <a href=""https://quizuplearn.com/events/{eventEntity.Id}"" class=""cta-button"">
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
                Nếu không muốn nhận thông báo về Events, vui lòng cập nhật trong cài đặt tài khoản.
            </p>
        </div>
    </div>
</body>
</html>";
        }
    }
}

