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
        private readonly RealtimeGameService _realtimeGameService;
        private readonly IMailerSendService _mailerSendService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<EventService> _logger;

        public EventService(
            IEventRepo eventRepo,
            IEventParticipantRepo eventParticipantRepo,
            IQuizSetRepo quizSetRepo,
            IUserRepo userRepo,
            IAccountRepo accountRepo,
            RealtimeGameService realtimeGameService,
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

            // ✨ TẠO GAME ROOM TRONG GAMEHUB
            var createGameDto = new CreateGameDto
            {
                QuizSetId = eventEntity.QuizSetId,
                HostUserId = userId,
                HostUserName = dto.HostUserName
            };

            var gameResponse = await _realtimeGameService.CreateGameAsync(createGameDto);

            // Update event status
            eventEntity.Status = "Active";
            await _eventRepo.UpdateAsync(eventEntity);

            _logger.LogInformation($"✅ Event started: {eventEntity.Name} (ID: {eventEntity.Id}), GamePin: {gameResponse.GamePin}");

            // ✨ GỬI EMAIL NOTIFICATION CHO TẤT CẢ USERS
            _ = Task.Run(async () =>
            {
                try
                {
                    await SendEventNotificationEmailsAsync(eventEntity, gameResponse.GamePin);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Failed to send email notifications for Event {eventEntity.Id}");
                }
            });

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
        /// Gửi email notification cho tất cả users trong hệ thống khi Event được start
        /// </summary>
        private async Task SendEventNotificationEmailsAsync(Event eventEntity, string gamePin)
        {
            try
            {
                // Lấy tất cả accounts trong hệ thống (chỉ lấy active accounts)
                var allAccounts = await _accountRepo.GetAllAsync(includeDeleted: false);
                var activeAccounts = allAccounts
                    .Where(a => a.IsActive && a.IsEmailVerified && !string.IsNullOrEmpty(a.Email))
                    .ToList();

                if (!activeAccounts.Any())
                {
                    _logger.LogWarning($"No active accounts found to send Event notification for Event {eventEntity.Id}");
                    return;
                }

                _logger.LogInformation($"Sending Event notification to {activeAccounts.Count} users for Event {eventEntity.Id}");

                // Lấy cấu hình email
                var fromEmail = _configuration["MailerSend:FromEmail"] ?? "no-reply@quizuplearn.com";
                var fromName = _configuration["MailerSend:FromName"] ?? "QuizUpLearn";

                // Tạo HTML template đẹp cho email
                var htmlTemplate = CreateEventNotificationEmailTemplate(eventEntity, gamePin);

                // Gửi email theo batch (MailerSend giới hạn số recipients per request)
                const int batchSize = 50; // MailerSend cho phép max 50 recipients per request
                var batches = activeAccounts
                    .Select((account, index) => new { account, index })
                    .GroupBy(x => x.index / batchSize)
                    .Select(g => g.Select(x => x.account).ToList())
                    .ToList();

                foreach (var batch in batches)
                {
                    try
                    {
                        var email = new MailerSendEmail
                        {
                            From = new MailerSendRecipient { Name = fromName, Email = fromEmail },
                            Subject = $"🎉 Event mới: {eventEntity.Name} - Tham gia ngay!",
                            Html = htmlTemplate,
                            Text = $"Event '{eventEntity.Name}' đã bắt đầu! Sử dụng GamePin: {gamePin} để tham gia."
                        };

                        // Thêm recipients vào batch
                        foreach (var account in batch)
                        {
                            var userName = account.User?.FullName ?? account.Email.Split('@')[0];
                            email.To.Add(new MailerSendRecipient
                            {
                                Name = userName,
                                Email = account.Email
                            });
                        }

                        await _mailerSendService.SendEmailAsync(email);
                        _logger.LogInformation($"✅ Sent email batch to {batch.Count} users for Event {eventEntity.Id}");

                        // Delay nhỏ giữa các batch để tránh rate limit
                        await Task.Delay(100);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Failed to send email batch for Event {eventEntity.Id}");
                    }
                }

                _logger.LogInformation($"✅ Completed sending Event notifications for Event {eventEntity.Id} to {activeAccounts.Count} users");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in SendEventNotificationEmailsAsync for Event {eventEntity.Id}");
                throw;
            }
        }

        /// <summary>
        /// Tạo HTML template đẹp cho email notification
        /// </summary>
        private string CreateEventNotificationEmailTemplate(Event eventEntity, string gamePin)
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

