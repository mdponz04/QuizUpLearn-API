using BusinessLogic.DTOs;
using BusinessLogic.DTOs.EventDtos;
using BusinessLogic.Interfaces;
using Repository.Entities;
using Repository.Enums;
using Repository.Interfaces;
using Microsoft.Extensions.Logging;

namespace BusinessLogic.Services
{
    public class EventService : IEventService
    {
        private readonly IEventRepo _eventRepo;
        private readonly IEventParticipantRepo _eventParticipantRepo;
        private readonly IQuizSetRepo _quizSetRepo;
        private readonly IUserRepo _userRepo;
        private readonly RealtimeGameService _realtimeGameService;
        private readonly ILogger<EventService> _logger;

        public EventService(
            IEventRepo eventRepo,
            IEventParticipantRepo eventParticipantRepo,
            IQuizSetRepo quizSetRepo,
            IUserRepo userRepo,
            RealtimeGameService realtimeGameService,
            ILogger<EventService> logger)
        {
            _eventRepo = eventRepo;
            _eventParticipantRepo = eventParticipantRepo;
            _quizSetRepo = quizSetRepo;
            _userRepo = userRepo;
            _realtimeGameService = realtimeGameService;
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
        /// Start Event - Tạo GameRoom trong GameHub
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
    }
}

