using BusinessLogic.DTOs;
using BusinessLogic.Interfaces;
using Repository.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace BusinessLogic.Services
{
    /// <summary>
    /// Service quản lý game 1vs1
    /// State được lưu trong Redis (Distributed Cache)
    /// </summary>
    public class OneVsOneGameService : IOneVsOneGameService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IDistributedCache _cache;
        private readonly ILogger<OneVsOneGameService> _logger;

        public OneVsOneGameService(
            IServiceProvider serviceProvider,
            IDistributedCache cache,
            ILogger<OneVsOneGameService> logger)
        {
            _serviceProvider = serviceProvider;
            _cache = cache;
            _logger = logger;
        }

        // ==================== REDIS HELPER METHODS ====================
        private async Task<OneVsOneRoomDto?> GetRoomFromRedisAsync(string roomPin)
        {
            try
            {
                var json = await _cache.GetStringAsync($"room1v1:{roomPin}");
                if (string.IsNullOrEmpty(json)) return null;
                
                return JsonSerializer.Deserialize<OneVsOneRoomDto>(json);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting room {roomPin} from Redis");
                return null;
            }
        }

        private async Task SaveRoomToRedisAsync(string roomPin, OneVsOneRoomDto room)
        {
            try
            {
                var json = JsonSerializer.Serialize(room);
                var options = new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(2)
                };
                
                await _cache.SetStringAsync($"room1v1:{roomPin}", json, options);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error saving room {roomPin} to Redis");
                throw;
            }
        }

        private async Task<Dictionary<Guid, bool>?> GetCorrectAnswersFromRedisAsync(string roomPin)
        {
            try
            {
                var json = await _cache.GetStringAsync($"answers1v1:{roomPin}");
                if (string.IsNullOrEmpty(json)) return null;
                
                return JsonSerializer.Deserialize<Dictionary<Guid, bool>>(json);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting correct answers for {roomPin} from Redis");
                return null;
            }
        }

        private async Task SaveCorrectAnswersToRedisAsync(string roomPin, Dictionary<Guid, bool> answers)
        {
            try
            {
                var json = JsonSerializer.Serialize(answers);
                var options = new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(2)
                };
                
                await _cache.SetStringAsync($"answers1v1:{roomPin}", json, options);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error saving correct answers for {roomPin} to Redis");
                throw;
            }
        }

        private async Task DeleteRoomFromRedisAsync(string roomPin)
        {
            try
            {
                await _cache.RemoveAsync($"room1v1:{roomPin}");
                await _cache.RemoveAsync($"answers1v1:{roomPin}");
                await _cache.RemoveAsync($"connection:{roomPin}"); // Mapping connection -> room
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting room {roomPin} from Redis");
            }
        }

        private async Task SaveConnectionMappingAsync(string connectionId, string roomPin)
        {
            try
            {
                await _cache.SetStringAsync($"connection1v1:{connectionId}", roomPin, new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(2)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error saving connection mapping for {connectionId}");
            }
        }

        // ==================== CREATE ROOM ====================
        public async Task<CreateOneVsOneRoomResponseDto> CreateRoomAsync(CreateOneVsOneRoomDto dto)
        {
            using var scope = _serviceProvider.CreateScope();
            var quizSetRepository = scope.ServiceProvider.GetRequiredService<IQuizSetRepo>();
            var quizRepository = scope.ServiceProvider.GetRequiredService<IQuizRepo>();
            var answerOptionRepository = scope.ServiceProvider.GetRequiredService<IAnswerOptionRepo>();

            // Validate quiz set exists
            var quizSet = await quizSetRepository.GetQuizSetByIdAsync(dto.QuizSetId);
            if (quizSet == null)
                throw new ArgumentException("Quiz set not found");

            // Load questions
            var quizzes = await quizRepository.GetQuizzesByQuizSetIdAsync(dto.QuizSetId);
            var questionsList = new List<QuestionDto>();
            var correctAnswersMap = new Dictionary<Guid, bool>();

            int questionNumber = 1;
            foreach (var quiz in quizzes)
            {
                var answerOptions = await answerOptionRepository.GetByQuizIdAsync(quiz.Id);
                
                var questionDto = new QuestionDto
                {
                    QuestionId = quiz.Id,
                    QuestionText = quiz.QuestionText,
                    ImageUrl = quiz.ImageURL,
                    AudioUrl = quiz.AudioURL,
                    QuestionNumber = questionNumber,
                    TotalQuestions = quizzes.Count(),
                    AnswerOptions = answerOptions.Select(ao => new AnswerOptionDto
                    {
                        AnswerId = ao.Id,
                        OptionText = ao.OptionText
                    }).ToList()
                };

                questionsList.Add(questionDto);

                // Lưu đáp án đúng
                foreach (var ao in answerOptions)
                {
                    correctAnswersMap[ao.Id] = ao.IsCorrect;
                }

                questionNumber++;
            }

            // Generate unique 6-digit Room PIN
            string roomPin;
            do
            {
                roomPin = new Random().Next(100000, 999999).ToString();
                var existingRoom = await GetRoomFromRedisAsync(roomPin);
                if (existingRoom == null) break;
            } while (true);

            var roomId = Guid.NewGuid();

            // Create room with Player1
            var room = new OneVsOneRoomDto
            {
                RoomPin = roomPin,
                RoomId = roomId,
                QuizSetId = dto.QuizSetId,
                Status = OneVsOneRoomStatus.Waiting,
                Player1 = new OneVsOnePlayerDto
                {
                    UserId = dto.Player1UserId, // Set UserId từ DTO
                    PlayerName = dto.Player1Name,
                    Score = 0,
                    CorrectAnswers = 0,
                    JoinedAt = DateTime.UtcNow,
                    IsReady = true
                },
                Questions = questionsList,
                CurrentQuestionIndex = 0,
                CreatedAt = DateTime.UtcNow
            };

            await SaveRoomToRedisAsync(roomPin, room);
            await SaveCorrectAnswersToRedisAsync(roomPin, correctAnswersMap);

            _logger.LogInformation($"✅ 1v1 Room created with PIN: {roomPin} by Player: {dto.Player1Name}");

            return new CreateOneVsOneRoomResponseDto
            {
                RoomPin = roomPin,
                RoomId = roomId,
                CreatedAt = room.CreatedAt
            };
        }

        // ==================== CONNECT & JOIN ====================
        public async Task<bool> PlayerConnectAsync(string roomPin, Guid userId, string connectionId)
        {
            var room = await GetRoomFromRedisAsync(roomPin);
            if (room == null)
            {
                _logger.LogWarning($"❌ Room {roomPin} not found in Redis");
                return false;
            }

            // Validate: Chỉ Player1 (người tạo phòng) mới được connect
            if (room.Player1 == null)
            {
                _logger.LogWarning($"❌ Room {roomPin} has no Player1");
                return false;
            }

            if (room.Player1.UserId == Guid.Empty)
            {
                _logger.LogWarning($"❌ Room {roomPin} Player1.UserId is empty (not set during room creation)");
                return false;
            }

            if (room.Player1.UserId != userId)
            {
                _logger.LogWarning($"❌ User {userId} tried to connect as Player1 but room creator is {room.Player1.UserId}. Room PIN: {roomPin}");
                return false;
            }

            // Update connectionId cho Player1
            room.Player1.ConnectionId = connectionId;

            // Lưu mapping connection -> room
            await SaveConnectionMappingAsync(connectionId, roomPin);
            await SaveRoomToRedisAsync(roomPin, room);

            _logger.LogInformation($"✅ Player1 (UserId: {userId}, Name: {room.Player1.PlayerName}) connected to room {roomPin}");
            return true;
        }

        public async Task<OneVsOnePlayerDto?> PlayerJoinAsync(string roomPin, Guid userId, string playerName, string connectionId)
        {
            var room = await GetRoomFromRedisAsync(roomPin);
            if (room == null)
                return null;

            // Check status
            if (room.Status != OneVsOneRoomStatus.Waiting)
                return null; // Room đã bắt đầu hoặc đã kết thúc

            // Validate: Player1 không được join bằng method này (phải dùng Player1Connect)
            if (room.Player1?.UserId == userId)
            {
                _logger.LogWarning($"❌ Player1 (UserId: {userId}) tried to join using Player2Join. Use Player1Connect instead.");
                return null;
            }

            // Check nếu đã có Player2
            if (room.Player2 != null)
                return null; // Room đã đầy

            // Add Player2
            var player2 = new OneVsOnePlayerDto
            {
                ConnectionId = connectionId,
                UserId = userId,
                PlayerName = playerName,
                Score = 0,
                CorrectAnswers = 0,
                JoinedAt = DateTime.UtcNow,
                IsReady = true
            };

            room.Player2 = player2;
            room.Status = OneVsOneRoomStatus.Ready; // Đủ 2 người, sẵn sàng start

            await SaveConnectionMappingAsync(connectionId, roomPin);
            await SaveRoomToRedisAsync(roomPin, room);

            _logger.LogInformation($"✅ Player2 '{playerName}' joined room {roomPin}");

            return player2;
        }

        public async Task<bool> PlayerLeaveAsync(string roomPin, string connectionId)
        {
            var room = await GetRoomFromRedisAsync(roomPin);
            if (room == null)
                return false;

            if (room.Player1?.ConnectionId == connectionId)
            {
                // Player1 leave → Cancel room
                room.Status = OneVsOneRoomStatus.Cancelled;
            }
            else if (room.Player2?.ConnectionId == connectionId)
            {
                // Player2 leave → Reset to Waiting
                room.Player2 = null;
                room.Status = OneVsOneRoomStatus.Waiting;
            }
            else
            {
                return false;
            }

            await SaveRoomToRedisAsync(roomPin, room);
            _logger.LogInformation($"Player left room {roomPin}");

            return true;
        }

        // ==================== START GAME ====================
        public async Task<bool> StartGameAsync(string roomPin)
        {
            var room = await GetRoomFromRedisAsync(roomPin);
            if (room == null)
                return false;

            if (room.Status != OneVsOneRoomStatus.Ready)
                return false; // Chưa đủ 2 người

            // Validate: Cả 2 players phải có connectionId (đã connect)
            if (room.Player1 == null || room.Player2 == null)
                return false;

            if (string.IsNullOrEmpty(room.Player1.ConnectionId) || string.IsNullOrEmpty(room.Player2.ConnectionId))
            {
                _logger.LogWarning($"❌ Cannot start game: One or both players not connected. P1: {!string.IsNullOrEmpty(room.Player1.ConnectionId)}, P2: {!string.IsNullOrEmpty(room.Player2.ConnectionId)}");
                return false;
            }

            room.Status = OneVsOneRoomStatus.InProgress;
            room.CurrentQuestionIndex = 0;
            room.QuestionStartedAt = DateTime.UtcNow;
            room.CurrentRoundResult = null;

            await SaveRoomToRedisAsync(roomPin, room);

            _logger.LogInformation($"✅ 1v1 Game started in room {roomPin}");

            return true;
        }

        // ==================== SUBMIT ANSWER ====================
        public async Task<OneVsOneRoundResultDto?> SubmitAnswerAsync(string roomPin, string connectionId, Guid questionId, Guid answerId)
        {
            var room = await GetRoomFromRedisAsync(roomPin);
            if (room == null)
                return null;

            if (room.Status != OneVsOneRoomStatus.InProgress)
                return null;

            if (!room.QuestionStartedAt.HasValue)
                return null;

            // Xác định player
            OneVsOnePlayerDto? player = null;
            if (room.Player1?.ConnectionId == connectionId)
                player = room.Player1;
            else if (room.Player2?.ConnectionId == connectionId)
                player = room.Player2;
            else
                return null;

            if (player == null)
                return null;

            // Check đáp án đúng
            bool isCorrect = false;
            var correctMap = await GetCorrectAnswersFromRedisAsync(roomPin);
            if (correctMap != null)
            {
                isCorrect = correctMap.GetValueOrDefault(answerId, false);
            }

            // Tính điểm: 1000 điểm cơ bản + bonus theo tốc độ (tương tự Kahoot)
            var timeSpent = (DateTime.UtcNow - room.QuestionStartedAt.Value).TotalSeconds;
            int points = 0;
            if (isCorrect)
            {
                const int MAX_TIME = 30;
                double timeRatio = Math.Max(0, 1.0 - (timeSpent / MAX_TIME));
                points = (int)(1000 + (timeRatio * 500)); // Tối đa 1500 điểm
                player.CorrectAnswers++;
            }

            player.Score += points;

            // Lưu answer vào current round result
            if (room.CurrentRoundResult == null)
            {
                room.CurrentRoundResult = new OneVsOneRoundResultDto
                {
                    QuestionId = questionId,
                    QuestionNumber = room.CurrentQuestionIndex + 1,
                    TotalQuestions = room.Questions.Count
                };

                // Tìm đáp án đúng
                var currentQuestion = room.Questions[room.CurrentQuestionIndex];
                foreach (var option in currentQuestion.AnswerOptions)
                {
                    if (correctMap?.GetValueOrDefault(option.AnswerId, false) == true)
                    {
                        room.CurrentRoundResult.CorrectAnswerId = option.AnswerId;
                        room.CurrentRoundResult.CorrectAnswerText = option.OptionText;
                        break;
                    }
                }
            }

            // Cập nhật kết quả player
            var playerResult = new OneVsOnePlayerResult
            {
                PlayerName = player.PlayerName,
                AnswerId = answerId,
                IsCorrect = isCorrect,
                PointsEarned = points,
                TimeSpent = timeSpent
            };

            if (player == room.Player1)
                room.CurrentRoundResult.Player1Result = playerResult;
            else
                room.CurrentRoundResult.Player2Result = playerResult;

            // Check xem cả 2 đã trả lời chưa
            bool bothAnswered = room.CurrentRoundResult.Player1Result != null && 
                               room.CurrentRoundResult.Player2Result != null;

            if (bothAnswered)
            {
                // Xác định winner của round này
                var p1Correct = room.CurrentRoundResult.Player1Result?.IsCorrect ?? false;
                var p2Correct = room.CurrentRoundResult.Player2Result?.IsCorrect ?? false;
                var p1Points = room.CurrentRoundResult.Player1Result?.PointsEarned ?? 0;
                var p2Points = room.CurrentRoundResult.Player2Result?.PointsEarned ?? 0;

                if (p1Correct && !p2Correct)
                    room.CurrentRoundResult.WinnerName = room.Player1?.PlayerName;
                else if (!p1Correct && p2Correct)
                    room.CurrentRoundResult.WinnerName = room.Player2?.PlayerName;
                else if (p1Correct && p2Correct)
                {
                    // Cả 2 đúng → Ai nhanh hơn thắng
                    if (p1Points > p2Points)
                        room.CurrentRoundResult.WinnerName = room.Player1?.PlayerName;
                    else if (p2Points > p1Points)
                        room.CurrentRoundResult.WinnerName = room.Player2?.PlayerName;
                    // Nếu bằng nhau thì không có winner
                }

                room.Status = OneVsOneRoomStatus.ShowingResult;
            }

            await SaveRoomToRedisAsync(roomPin, room);

            _logger.LogInformation($"✅ Player '{player.PlayerName}' submitted answer. Both answered: {bothAnswered}");

            // Trả về result nếu cả 2 đã trả lời
            return bothAnswered ? room.CurrentRoundResult : null;
        }

        // ==================== NEXT QUESTION ====================
        public async Task<bool> NextQuestionAsync(string roomPin)
        {
            var room = await GetRoomFromRedisAsync(roomPin);
            if (room == null)
            {
                _logger.LogWarning($"❌ NextQuestionAsync: Room {roomPin} not found");
                return false;
            }

            _logger.LogInformation($"🔄 NextQuestionAsync: Room {roomPin}, CurrentStatus: {room.Status}, CurrentIndex: {room.CurrentQuestionIndex}, TotalQuestions: {room.Questions.Count}");

            if (room.Status != OneVsOneRoomStatus.ShowingResult)
            {
                _logger.LogWarning($"❌ NextQuestionAsync: Room {roomPin} status is {room.Status}, expected ShowingResult");
                return false;
            }

            room.CurrentQuestionIndex++;

            // Check nếu hết câu hỏi
            if (room.CurrentQuestionIndex >= room.Questions.Count)
            {
                _logger.LogInformation($"✅ NextQuestionAsync: Room {roomPin} - No more questions, ending game");
                room.Status = OneVsOneRoomStatus.Completed;
                await SaveRoomToRedisAsync(roomPin, room);
                return false; // Hết câu hỏi
            }

            // Reset state cho câu hỏi mới
            room.Status = OneVsOneRoomStatus.InProgress;
            room.QuestionStartedAt = DateTime.UtcNow;
            room.CurrentRoundResult = null;

            await SaveRoomToRedisAsync(roomPin, room);

            _logger.LogInformation($"✅ Room {roomPin} moved to question {room.CurrentQuestionIndex + 1}");

            return true;
        }

        // ==================== GET STATE ====================
        public async Task<OneVsOneRoomDto?> GetRoomAsync(string roomPin)
        {
            return await GetRoomFromRedisAsync(roomPin);
        }

        public async Task<string?> GetRoomPinByConnectionAsync(string connectionId)
        {
            try
            {
                return await _cache.GetStringAsync($"connection1v1:{connectionId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting room PIN for connection {connectionId}");
                return null;
            }
        }

        // ==================== RESULT MANAGEMENT ====================
        /// <summary>
        /// Lấy kết quả round hiện tại (kể cả khi chưa đủ 2 người trả lời)
        /// </summary>
        public async Task<OneVsOneRoundResultDto?> GetCurrentRoundResultAsync(string roomPin)
        {
            var room = await GetRoomFromRedisAsync(roomPin);
            if (room == null)
                return null;

            if (room.CurrentRoundResult == null)
            {
                // Nếu chưa có result, tạo một result rỗng với thông tin câu hỏi
                if (room.CurrentQuestionIndex >= room.Questions.Count)
                    return null;

                var currentQuestion = room.Questions[room.CurrentQuestionIndex];
                var correctMap = await GetCorrectAnswersFromRedisAsync(roomPin);

                var result = new OneVsOneRoundResultDto
                {
                    QuestionId = currentQuestion.QuestionId,
                    QuestionNumber = room.CurrentQuestionIndex + 1,
                    TotalQuestions = room.Questions.Count
                };

                // Tìm đáp án đúng
                foreach (var option in currentQuestion.AnswerOptions)
                {
                    if (correctMap?.GetValueOrDefault(option.AnswerId, false) == true)
                    {
                        result.CorrectAnswerId = option.AnswerId;
                        result.CorrectAnswerText = option.OptionText;
                        break;
                    }
                }

                return result;
            }

            return room.CurrentRoundResult;
        }

        /// <summary>
        /// Đánh dấu đã show result (chuyển status sang ShowingResult)
        /// </summary>
        public async Task MarkResultShownAsync(string roomPin)
        {
            var room = await GetRoomFromRedisAsync(roomPin);
            if (room == null)
            {
                _logger.LogWarning($"❌ MarkResultShownAsync: Room {roomPin} not found");
                return;
            }

            _logger.LogInformation($"🔄 MarkResultShownAsync: Room {roomPin}, CurrentStatus: {room.Status}");

            if (room.Status == OneVsOneRoomStatus.InProgress)
            {
                room.Status = OneVsOneRoomStatus.ShowingResult;
                await SaveRoomToRedisAsync(roomPin, room);
                _logger.LogInformation($"✅ Room {roomPin} result marked as shown (Status: InProgress → ShowingResult)");
            }
            else
            {
                _logger.LogWarning($"⚠️ MarkResultShownAsync: Room {roomPin} status is {room.Status}, not InProgress. Skipping status change.");
            }
        }

        // ==================== GAME END ====================
        public async Task<OneVsOneFinalResultDto?> GetFinalResultAsync(string roomPin)
        {
            var room = await GetRoomFromRedisAsync(roomPin);
            if (room == null)
                return null;

            if (room.Status != OneVsOneRoomStatus.Completed)
                return null;

            OneVsOnePlayerDto? winner = null;
            if (room.Player1 != null && room.Player2 != null)
            {
                if (room.Player1.Score > room.Player2.Score)
                    winner = room.Player1;
                else if (room.Player2.Score > room.Player1.Score)
                    winner = room.Player2;
                // Nếu bằng nhau thì winner = null
            }

            return new OneVsOneFinalResultDto
            {
                RoomPin = roomPin,
                Winner = winner,
                Player1 = room.Player1,
                Player2 = room.Player2,
                TotalQuestions = room.Questions.Count,
                CompletedAt = DateTime.UtcNow
            };
        }

        public async Task CleanupRoomAsync(string roomPin)
        {
            await DeleteRoomFromRedisAsync(roomPin);
            _logger.LogInformation($"✅ Room {roomPin} cleaned up from Redis");
        }
    }
}

