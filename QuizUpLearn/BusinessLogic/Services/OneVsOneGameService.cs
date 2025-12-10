using BusinessLogic.DTOs;
using BusinessLogic.Interfaces;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Repository.Enums;
using Repository.Interfaces;
using System.Text.Json;

namespace BusinessLogic.Services
{
    /// <summary>
    /// Service quản lý game 1vs1 và Multiplayer
    /// State được lưu trong Redis (Distributed Cache)
    /// Hỗ trợ cả 2 chế độ: OneVsOne (2 players) và Multiplayer (unlimited)
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
            var quizGroupItemRepository = scope.ServiceProvider.GetRequiredService<IQuizGroupItemRepo>();

            // Validate quiz set exists
            var quizSet = await quizSetRepository.GetQuizSetByIdAsync(dto.QuizSetId);
            if (quizSet == null)
                throw new ArgumentException("Quiz set not found");

            // Load quiz group items for TOEIC-style grouped questions (Parts 3,4,6,7)
            //var quizGroupItems = await quizGroupItemRepository.GetAllByQuizSetIdAsync(dto.QuizSetId);
            /*var quizGroupItemsMap = new Dictionary<Guid, QuizGroupItemDto>();
            foreach (var groupItem in quizGroupItems)
            {
                quizGroupItemsMap[groupItem.Id] = new QuizGroupItemDto
                {
                    Id = groupItem.Id,
                    Name = groupItem.Name,
                    AudioUrl = groupItem.AudioUrl,
                    ImageUrl = groupItem.ImageUrl,
                    PassageText = groupItem.PassageText
                };
            }*/

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
                    QuizGroupItemId = quiz.QuizGroupItemId, // Reference to group item (TOEIC Parts 3,4,6,7)
                    ToeicPart = quiz.TOEICPart, // TOEIC Part for UI logic (Parts 1,2,5 don't show group)
                    AnswerOptions = answerOptions.Select(ao => new AnswerOptionDto
                    {
                        AnswerId = ao.Id,
                        OptionLabel = ao.OptionLabel,
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

            // Create Player1 (Host)
            var player1 = new OneVsOnePlayerDto
            {
                UserId = dto.Player1UserId,
                PlayerName = dto.Player1Name,
                Score = 0,
                CorrectAnswers = 0,
                JoinedAt = DateTime.UtcNow,
                IsReady = true
            };

            // ✨ Auto-set MaxPlayers dựa trên Mode
            int? maxPlayers = dto.Mode == GameModeEnum.OneVsOne ? 2 : null;

            // Create room
            var room = new OneVsOneRoomDto
            {
                RoomPin = roomPin,
                RoomId = roomId,
                QuizSetId = dto.QuizSetId,
                Status = OneVsOneRoomStatus.Waiting,
                
                // ✨ Mode & Auto MaxPlayers
                Mode = dto.Mode,
                MaxPlayers = maxPlayers,
                
                // ✨ Universal Players list
                Players = new List<OneVsOnePlayerDto> { player1 },
                
                // Backward compatibility
                Player1 = player1,
                
                Questions = questionsList,
                //QuizGroupItems = quizGroupItemsMap, // Store group items for TOEIC-style questions
                CurrentQuestionIndex = 0,
                CurrentAnswers = new Dictionary<string, OneVsOneAnswerDto>(),
                CreatedAt = DateTime.UtcNow
            };

            await SaveRoomToRedisAsync(roomPin, room);
            await SaveCorrectAnswersToRedisAsync(roomPin, correctAnswersMap);

            var maxPlayersText = maxPlayers.HasValue ? $"{maxPlayers} players max" : "unlimited players";
            _logger.LogInformation($"✅ {dto.Mode} Room created with PIN: {roomPin} by Player: {dto.Player1Name} ({maxPlayersText})");
            //_logger.LogInformation($"📦 Loaded {quizGroupItemsMap.Count} quiz group items for grouped questions");

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

            // Update connectionId cho Player1 (handle reconnection)
            var oldConnectionId = room.Player1.ConnectionId;
            var isReconnecting = !string.IsNullOrEmpty(oldConnectionId) && oldConnectionId != connectionId;
            
            if (isReconnecting)
            {
                _logger.LogInformation($"🔄 Player1 '{room.Player1.PlayerName}' is RECONNECTING to room {roomPin}");
                _logger.LogInformation($"   Old ConnectionId: {oldConnectionId}");
                _logger.LogInformation($"   New ConnectionId: {connectionId}");
            }
            
            room.Player1.ConnectionId = connectionId;
            
            // ✨ NEW: Update trong Players list
            var player1InList = room.Players.FirstOrDefault(p => p.UserId == userId);
            if (player1InList != null)
            {
                player1InList.ConnectionId = connectionId;
            }
            
            if (isReconnecting)
            {
                _logger.LogInformation($"✅ Player1 '{room.Player1.PlayerName}' ConnectionId updated successfully");
            }

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
            if (room.Status != OneVsOneRoomStatus.Waiting && room.Status != OneVsOneRoomStatus.Ready)
                return null; // Room đã bắt đầu hoặc đã kết thúc

            // Validate: Player1 không được join bằng method này (phải dùng Player1Connect)
            if (room.Player1?.UserId == userId)
            {
                _logger.LogWarning($"❌ Player1 (UserId: {userId}) tried to join using PlayerJoin. Use Player1Connect instead.");
                return null;
            }

            // ✨ NEW: Check MaxPlayers
            if (room.MaxPlayers.HasValue && room.Players.Count >= room.MaxPlayers.Value)
            {
                _logger.LogWarning($"❌ Room {roomPin} is full ({room.Players.Count}/{room.MaxPlayers.Value})");
                return null;
            }

            // Check if player is reconnecting (same userId but different connectionId)
            var existingPlayer = room.Players.FirstOrDefault(p => p.UserId == userId);
            if (existingPlayer != null)
            {
                _logger.LogInformation($"🔄 Player '{existingPlayer.PlayerName}' (UserId: {userId}) is RECONNECTING to room {roomPin}");
                _logger.LogInformation($"   Old ConnectionId: {existingPlayer.ConnectionId}");
                _logger.LogInformation($"   New ConnectionId: {connectionId}");
                
                // Update ConnectionId for reconnection
                existingPlayer.ConnectionId = connectionId;
                await SaveRoomToRedisAsync(roomPin, room);
                
                _logger.LogInformation($"✅ Player '{existingPlayer.PlayerName}' ConnectionId updated successfully");
                return existingPlayer;
            }

            // Check duplicate name
            if (room.Players.Any(p => p.PlayerName.Equals(playerName, StringComparison.OrdinalIgnoreCase)))
            {
                _logger.LogWarning($"❌ Player name '{playerName}' already taken in room {roomPin}");
                throw new InvalidOperationException($"DUPLICATE_NAME:{playerName}");
            }

            // Add new player
            var newPlayer = new OneVsOnePlayerDto
            {
                ConnectionId = connectionId,
                UserId = userId,
                PlayerName = playerName,
                Score = 0,
                CorrectAnswers = 0,
                JoinedAt = DateTime.UtcNow,
                IsReady = true
            };

            room.Players.Add(newPlayer);

            // ✨ NEW: Update status based on Mode
            if (room.Mode == GameModeEnum.OneVsOne)
            {
                // 1vs1: Ready khi đủ 2 người
                if (room.Players.Count == 2)
                {
                    room.Player2 = newPlayer; // Backward compat
                    room.Status = OneVsOneRoomStatus.Ready;
                }
            }
            else if (room.Mode == GameModeEnum.Multiplayer)
            {
                // Multiplayer: Ready khi >= 2 người
                if (room.Players.Count >= 2)
                {
                    room.Status = OneVsOneRoomStatus.Ready;
                }
                
                // Update Player2 cho backward compat nếu có
                if (room.Players.Count >= 2 && room.Player2 == null)
                {
                    room.Player2 = room.Players[1];
                }
            }

            await SaveConnectionMappingAsync(connectionId, roomPin);
            await SaveRoomToRedisAsync(roomPin, room);

            _logger.LogInformation($"✅ Player '{playerName}' joined {room.Mode} room {roomPin} ({room.Players.Count}/{room.MaxPlayers?.ToString() ?? "∞"} players)");

            return newPlayer;
        }

        /// <summary>
        /// Reconnect a player to an active game (update ConnectionId without re-joining)
        /// Used when player refreshes browser or loses connection during gameplay
        /// </summary>
        public async Task<bool> ReconnectPlayerAsync(string roomPin, Guid userId, string newConnectionId)
        {
            var room = await GetRoomFromRedisAsync(roomPin);
            if (room == null)
            {
                _logger.LogWarning($"❌ Room {roomPin} not found for reconnection");
                return false;
            }

            // Find player by userId (works for any game status)
            var player = room.Players.FirstOrDefault(p => p.UserId == userId);
            if (player == null)
            {
                _logger.LogWarning($"❌ Player with UserId {userId} not found in room {roomPin}");
                return false;
            }

            var oldConnectionId = player.ConnectionId;
            _logger.LogInformation($"🔄 RECONNECT - Player '{player.PlayerName}' (UserId: {userId}) in room {roomPin}");
            _logger.LogInformation($"   Game Status: {room.Status}");
            _logger.LogInformation($"   Old ConnectionId: {oldConnectionId}");
            _logger.LogInformation($"   New ConnectionId: {newConnectionId}");

            // Update ConnectionId
            player.ConnectionId = newConnectionId;

            // Update Player1/Player2 refs if needed
            if (room.Player1?.UserId == userId)
            {
                room.Player1.ConnectionId = newConnectionId;
            }
            if (room.Player2?.UserId == userId)
            {
                room.Player2.ConnectionId = newConnectionId;
            }

            // Update connection mapping
            await SaveConnectionMappingAsync(newConnectionId, roomPin);
            await SaveRoomToRedisAsync(roomPin, room);

            _logger.LogInformation($"✅ Player '{player.PlayerName}' ConnectionId updated successfully - can now submit answers");
            return true;
        }

        public async Task<bool> PlayerLeaveAsync(string roomPin, string connectionId)
        {
            var room = await GetRoomFromRedisAsync(roomPin);
            if (room == null)
                return false;

            var leavingPlayer = room.Players.FirstOrDefault(p => p.ConnectionId == connectionId);
            if (leavingPlayer == null)
                return false;

            // Player1 (Host) leave → Cancel room
            if (room.Player1?.ConnectionId == connectionId)
            {
                room.Status = OneVsOneRoomStatus.Cancelled;
                _logger.LogInformation($"Host left room {roomPin} - Room cancelled");
            }
            else
            {
                // Other players leave → Remove from list
                room.Players.Remove(leavingPlayer);
                
                // Update status
                if (room.Players.Count < 2)
                {
                    room.Status = OneVsOneRoomStatus.Waiting;
                }
                
                // Update backward compat fields
                if (room.Player2?.ConnectionId == connectionId)
                {
                    room.Player2 = room.Players.Count >= 2 ? room.Players[1] : null;
                }

                _logger.LogInformation($"Player '{leavingPlayer.PlayerName}' left room {roomPin} ({room.Players.Count} players remaining)");
            }

            await SaveRoomToRedisAsync(roomPin, room);
            return true;
        }

        // ==================== START GAME ====================
        public async Task<bool> StartGameAsync(string roomPin)
        {
            var room = await GetRoomFromRedisAsync(roomPin);
            if (room == null)
                return false;

            if (room.Status != OneVsOneRoomStatus.Ready)
                return false;

            // ✨ NEW: Validate tất cả players đã connect
            var minPlayers = room.Mode == GameModeEnum.OneVsOne ? 2 : 2; // Cả 2 modes đều cần ít nhất 2 players
            if (room.Players.Count < minPlayers)
            {
                _logger.LogWarning($"❌ Cannot start game: Not enough players ({room.Players.Count}/{minPlayers})");
                return false;
            }

            // Validate: Tất cả players phải có connectionId
            var playersNotConnected = room.Players.Where(p => string.IsNullOrEmpty(p.ConnectionId)).ToList();
            if (playersNotConnected.Any())
            {
                _logger.LogWarning($"❌ Cannot start game: {playersNotConnected.Count} player(s) not connected yet");
                return false;
            }

            room.Status = OneVsOneRoomStatus.InProgress;
            room.CurrentQuestionIndex = 0;
            room.QuestionStartedAt = DateTime.UtcNow;
            room.CurrentRoundResult = null;
            room.CurrentAnswers.Clear(); // Clear previous answers

            await SaveRoomToRedisAsync(roomPin, room);

            _logger.LogInformation($"✅ {room.Mode} Game started in room {roomPin} with {room.Players.Count} players");

            return true;
        }

        // ==================== SUBMIT ANSWER ====================
        public async Task<OneVsOneRoundResultDto?> SubmitAnswerAsync(string roomPin, string connectionId, Guid questionId, Guid answerId)
        {
            _logger.LogInformation($"🎯 SubmitAnswerAsync START - Room: {roomPin}, ConnectionId: {connectionId}, QuestionId: {questionId}, AnswerId: {answerId}");
            
            var room = await GetRoomFromRedisAsync(roomPin);
            if (room == null)
            {
                _logger.LogWarning($"❌ Room {roomPin} not found in Redis");
                return null;
            }

            if (room.Status != OneVsOneRoomStatus.InProgress)
            {
                _logger.LogWarning($"❌ Room {roomPin} is not in progress (Status: {room.Status})");
                return null;
            }

            if (!room.QuestionStartedAt.HasValue)
            {
                _logger.LogWarning($"❌ Room {roomPin} has no QuestionStartedAt");
                return null;
            }

            // Log all players in room
            _logger.LogInformation($"📋 Room has {room.Players.Count} players:");
            foreach (var p in room.Players)
            {
                _logger.LogInformation($"  - {p.PlayerName} (ConnectionId: {p.ConnectionId})");
            }

            // ✨ NEW: Tìm player trong Players list
            var player = room.Players.FirstOrDefault(p => p.ConnectionId == connectionId);
            if (player == null)
            {
                _logger.LogWarning($"❌ Player with ConnectionId {connectionId} NOT FOUND in room.Players!");
                _logger.LogWarning($"❌ Available ConnectionIds: {string.Join(", ", room.Players.Select(p => p.ConnectionId))}");
                return null;
            }
            
            _logger.LogInformation($"✅ Found player: {player.PlayerName}");

            // Check đã trả lời chưa
            if (room.CurrentAnswers.ContainsKey(connectionId))
            {
                _logger.LogWarning($"❌ Player '{player.PlayerName}' already answered this question");
                return null;
            }

            _logger.LogInformation($"✅ Player '{player.PlayerName}' has NOT answered yet. Current answers: {room.CurrentAnswers.Count}/{room.Players.Count}");

            // Check đáp án đúng
            bool isCorrect = false;
            var correctMap = await GetCorrectAnswersFromRedisAsync(roomPin);
            if (correctMap != null)
            {
                isCorrect = correctMap.GetValueOrDefault(answerId, false);
            }
            
            _logger.LogInformation($"✅ Answer checked. IsCorrect: {isCorrect}");

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

            // Lưu answer vào dictionary
            var answer = new OneVsOneAnswerDto
            {
                ConnectionId = connectionId,
                UserId = player.UserId,
                PlayerName = player.PlayerName,
                QuestionId = questionId,
                AnswerId = answerId,
                IsCorrect = isCorrect,
                PointsEarned = points,
                TimeSpent = timeSpent,
                SubmittedAt = DateTime.UtcNow
            };

            room.CurrentAnswers[connectionId] = answer;
            
            _logger.LogInformation($"✅ Answer stored in CurrentAnswers. Total: {room.CurrentAnswers.Count}/{room.Players.Count}");

            // ✨ NEW: Check xem tất cả players đã trả lời chưa
            bool allAnswered = room.Players.All(p => room.CurrentAnswers.ContainsKey(p.ConnectionId));
            
            // Log who answered and who didn't
            var answeredPlayers = room.Players.Where(p => room.CurrentAnswers.ContainsKey(p.ConnectionId)).Select(p => p.PlayerName).ToList();
            var notAnsweredPlayers = room.Players.Where(p => !room.CurrentAnswers.ContainsKey(p.ConnectionId)).Select(p => p.PlayerName).ToList();
            
            _logger.LogInformation($"✅ Answered players: {string.Join(", ", answeredPlayers)}");
            if (notAnsweredPlayers.Any())
            {
                _logger.LogInformation($"⏳ Waiting for: {string.Join(", ", notAnsweredPlayers)}");
            }

            if (allAnswered)
            {
                _logger.LogInformation($"🎉 ALL PLAYERS ANSWERED! Building round result...");
                // Tạo round result
                room.CurrentRoundResult = await BuildRoundResultAsync(room, correctMap);
                room.Status = OneVsOneRoomStatus.ShowingResult;
            }

            await SaveRoomToRedisAsync(roomPin, room);

            _logger.LogInformation($"✅ Player '{player.PlayerName}' submitted answer ({room.CurrentAnswers.Count}/{room.Players.Count}). All answered: {allAnswered}");
            _logger.LogInformation($"🎯 SubmitAnswerAsync END - Returning {(allAnswered ? "RESULT" : "NULL")}");

            // Trả về result nếu tất cả đã trả lời
            return allAnswered ? room.CurrentRoundResult : null;
        }

        /// <summary>
        /// Build round result từ CurrentAnswers - CHỈ những người đã submit
        /// </summary>
        private async Task<OneVsOneRoundResultDto> BuildRoundResultAsync(OneVsOneRoomDto room, Dictionary<Guid, bool>? correctMap)
        {
            var currentQuestion = room.Questions[room.CurrentQuestionIndex];
            
            // Tìm đáp án đúng
            Guid correctAnswerId = Guid.Empty;
            string correctAnswerText = string.Empty;
            foreach (var option in currentQuestion.AnswerOptions)
            {
                if (correctMap?.GetValueOrDefault(option.AnswerId, false) == true)
                {
                    correctAnswerId = option.AnswerId;
                    correctAnswerText = option.OptionText;
                    break;
                }
            }

            // Players chưa submit sẽ KHÔNG có trong kết quả
            var playerResults = room.CurrentAnswers.Values
                .OrderByDescending(a => a.PointsEarned)
                .ThenBy(a => a.TimeSpent)
                .Select(a => new OneVsOnePlayerResult
                {
                    PlayerName = a.PlayerName,
                    AnswerId = a.AnswerId,
                    IsCorrect = a.IsCorrect,
                    PointsEarned = a.PointsEarned,
                    TimeSpent = a.TimeSpent
                })
                .ToList();

            _logger.LogInformation($"BuildRoundResult: {playerResults.Count} players submitted answers out of {room.Players.Count} total players");
            
            // Log chi tiết để debug
            foreach (var r in playerResults)
            {
                _logger.LogInformation($"  - {r.PlayerName}: {r.PointsEarned} pts, IsCorrect: {r.IsCorrect}");
            }
            
            // Log players chưa submit
            var playersNotAnswered = room.Players
                .Where(p => !room.CurrentAnswers.ContainsKey(p.ConnectionId))
                .Select(p => p.PlayerName)
                .ToList();
            
            if (playersNotAnswered.Any())
            {
                _logger.LogWarning($"Players DID NOT submit answer: {string.Join(", ", playersNotAnswered)}");
            }

            // Xác định winner (người có điểm cao nhất trong số những người đã trả lời)
            var topResult = playerResults.FirstOrDefault();
            string? winnerName = null;
            if (topResult != null && topResult.PointsEarned > 0 && topResult.IsCorrect)
            {
                // Nếu có nhiều người cùng điểm cao nhất thì không có winner
                var topPoints = topResult.PointsEarned;
                var topCount = playerResults.Count(r => r.PointsEarned == topPoints);
                winnerName = topCount == 1 ? topResult.PlayerName : null;
            }

            var result = new OneVsOneRoundResultDto
            {
                QuestionId = currentQuestion.QuestionId,
                QuestionNumber = room.CurrentQuestionIndex + 1,
                TotalQuestions = room.Questions.Count,
                CorrectAnswerId = correctAnswerId,
                CorrectAnswerText = correctAnswerText,
                PlayerResults = playerResults, 
                WinnerName = winnerName
            };

            // Backward compatibility
            result.Player1Result = playerResults.FirstOrDefault(r => r.PlayerName == room.Player1?.PlayerName);
            result.Player2Result = playerResults.FirstOrDefault(r => r.PlayerName == room.Player2?.PlayerName);

            return result;
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
            room.CurrentAnswers.Clear();

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
        /// Lấy kết quả round hiện tại (kể cả khi chưa đủ người trả lời)
        /// </summary>
        public async Task<OneVsOneRoundResultDto?> GetCurrentRoundResultAsync(string roomPin)
        {
            var room = await GetRoomFromRedisAsync(roomPin);
            if (room == null)
                return null;

            // Nếu đã có result thì return luôn
            if (room.CurrentRoundResult != null)
                return room.CurrentRoundResult;

            // Nếu có người đã trả lời thì build result từ CurrentAnswers
            if (room.CurrentAnswers.Count > 0)
            {
                var correctMap = await GetCorrectAnswersFromRedisAsync(roomPin);
                return await BuildRoundResultAsync(room, correctMap);
            }

            // Nếu chưa có ai trả lời, tạo result rỗng với thông tin câu hỏi
            if (room.CurrentQuestionIndex >= room.Questions.Count)
                return null;

            var currentQuestion = room.Questions[room.CurrentQuestionIndex];
            var correctMapEmpty = await GetCorrectAnswersFromRedisAsync(roomPin);

            var result = new OneVsOneRoundResultDto
            {
                QuestionId = currentQuestion.QuestionId,
                QuestionNumber = room.CurrentQuestionIndex + 1,
                TotalQuestions = room.Questions.Count,
                PlayerResults = new List<OneVsOnePlayerResult>()
            };

            // Tìm đáp án đúng
            foreach (var option in currentQuestion.AnswerOptions)
            {
                if (correctMapEmpty?.GetValueOrDefault(option.AnswerId, false) == true)
                {
                    result.CorrectAnswerId = option.AnswerId;
                    result.CorrectAnswerText = option.OptionText;
                    break;
                }
            }

            return result;
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

            // ✨ NEW: Rankings sorted by score descending
            var rankings = room.Players
                .OrderByDescending(p => p.Score)
                .ThenByDescending(p => p.CorrectAnswers)
                .ThenBy(p => p.JoinedAt)
                .ToList();

            var winner = rankings.FirstOrDefault();
            
            // Nếu có nhiều người cùng điểm cao nhất thì không có winner
            if (winner != null && rankings.Count(p => p.Score == winner.Score) > 1)
            {
                winner = null;
            }

            return new OneVsOneFinalResultDto
            {
                RoomPin = roomPin,
                Mode = room.Mode,
                Rankings = rankings,
                Winner = winner,
                
                // Backward compatibility
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

