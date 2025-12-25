using BusinessLogic.DTOs;
using BusinessLogic.Interfaces;
using Repository.Interfaces;
using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;
using Repository.Enums;

namespace BusinessLogic.Services
{
    /// <summary>
    /// Service quản lý Kahoot-style realtime quiz game
    /// State được lưu trong Redis (Distributed Cache) 
    /// Hỗ trợ scale-out multiple servers
    /// </summary>
    public class RealtimeGameService : BusinessLogic.Interfaces.IRealtimeGameService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IDistributedCache _cache; // ✨ Redis Cache
        private readonly ILogger<RealtimeGameService> _logger;

        public RealtimeGameService(
            IServiceProvider serviceProvider,
            IDistributedCache cache, // ✨ Inject Redis
            ILogger<RealtimeGameService> logger)
        {
            _serviceProvider = serviceProvider;
            _cache = cache; // ✨ Redis
            _logger = logger;
        }

        // ==================== REDIS HELPER METHODS ====================
        private async Task<GameSessionDto?> GetGameSessionFromRedisAsync(string gamePin)
        {
            try
            {
                var json = await _cache.GetStringAsync($"game:{gamePin}");
                if (string.IsNullOrEmpty(json)) return null;
                
                return JsonSerializer.Deserialize<GameSessionDto>(json);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting game session {gamePin} from Redis");
                return null;
            }
        }

        private async Task SaveGameSessionToRedisAsync(string gamePin, GameSessionDto session)
        {
            try
            {
                var json = JsonSerializer.Serialize(session);
                var options = new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(2)
                };
                
                await _cache.SetStringAsync($"game:{gamePin}", json, options);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error saving game session {gamePin} to Redis");
                throw;
            }
        }

        private async Task<Dictionary<Guid, bool>?> GetCorrectAnswersFromRedisAsync(string gamePin)
        {
            try
            {
                var json = await _cache.GetStringAsync($"answers:{gamePin}");
                if (string.IsNullOrEmpty(json)) return null;
                
                return JsonSerializer.Deserialize<Dictionary<Guid, bool>>(json);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting correct answers for {gamePin} from Redis");
                return null;
            }
        }

        private async Task SaveCorrectAnswersToRedisAsync(string gamePin, Dictionary<Guid, bool> answers)
        {
            try
            {
                var json = JsonSerializer.Serialize(answers);
                var options = new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(2)
                };
                
                await _cache.SetStringAsync($"answers:{gamePin}", json, options);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error saving correct answers for {gamePin} to Redis");
                throw;
            }
        }

        private async Task DeleteGameFromRedisAsync(string gamePin)
        {
            try
            {
                await _cache.RemoveAsync($"game:{gamePin}");
                await _cache.RemoveAsync($"answers:{gamePin}");
                await _cache.RemoveAsync($"final:{gamePin}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting game {gamePin} from Redis");
            }
        }

        private async Task SaveFinalResultToRedisAsync(string gamePin, FinalResultDto finalResult)
        {
            try
            {
                var json = JsonSerializer.Serialize(finalResult);
                var options = new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(2)
                };
                await _cache.SetStringAsync($"final:{gamePin}", json, options);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error saving final result for {gamePin} to Redis");
            }
        }

        private async Task<FinalResultDto?> GetCachedFinalResultFromRedisAsync(string gamePin)
        {
            try
            {
                var json = await _cache.GetStringAsync($"final:{gamePin}");
                if (string.IsNullOrEmpty(json)) return null;
                return JsonSerializer.Deserialize<FinalResultDto>(json);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting cached final result for {gamePin} from Redis");
                return null;
            }
        }

        /// <summary>
        /// Get correct answers for a specific question (filters from full correct answers map)
        /// </summary>
        public async Task<Dictionary<Guid, bool>?> GetCorrectAnswersForQuestionAsync(string gamePin, Guid questionId)
        {
            var session = await GetGameSessionFromRedisAsync(gamePin);
            if (session == null) return null;

            var question = session.Questions.FirstOrDefault(q => q.QuestionId == questionId);
            if (question == null) return null;

            var allCorrectAnswers = await GetCorrectAnswersFromRedisAsync(gamePin);
            if (allCorrectAnswers == null) return null;

            // Filter to only answers for this question
            var questionAnswerIds = question.AnswerOptions.Select(a => a.AnswerId).ToHashSet();
            var filteredAnswers = allCorrectAnswers
                .Where(kvp => questionAnswerIds.Contains(kvp.Key))
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

            return filteredAnswers;
        }

        // ==================== HOST CREATES GAME ====================
        /// <summary>
        /// Host tạo game session và nhận Game PIN
        /// </summary>
        public async Task<CreateGameResponseDto> CreateGameAsync(CreateGameDto dto)
        {
            // Tạo scope để resolve scoped services
            using var scope = _serviceProvider.CreateScope();
            var quizSetRepository = scope.ServiceProvider.GetRequiredService<IQuizSetRepo>();
            var quizRepository = scope.ServiceProvider.GetRequiredService<IQuizRepo>();
            var answerOptionRepository = scope.ServiceProvider.GetRequiredService<IAnswerOptionRepo>();
            var quizGroupItemRepository = scope.ServiceProvider.GetRequiredService<IQuizGroupItemRepo>();

            // Validate quiz set exists
            var quizSet = await quizSetRepository.GetQuizSetByIdAsync(dto.QuizSetId);
            if (quizSet == null)
                throw new ArgumentException("Quiz set not found");

            // Load questions (đã include QuizGroupItem từ repository)
            var quizzes = await quizRepository.GetQuizzesByQuizSetIdAsync(dto.QuizSetId);
            var questionsList = new List<QuestionDto>();
            var correctAnswersMap = new Dictionary<Guid, bool>();
            
            // ✨ Build QuizGroupItems map from quizzes (for TOEIC-style grouped questions)
            var quizGroupItemsMap = new Dictionary<Guid, QuizGroupItemDto>();

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
                    TimeLimit = null, // Host sẽ set theo từng câu
                    QuizGroupItemId = quiz.QuizGroupItemId, // Reference to group item (TOEIC Parts 3,4,6,7)
                    ToeicPart = quiz.TOEICPart, // TOEIC Part for UI logic
                    AnswerOptions = answerOptions.Select(ao => new AnswerOptionDto
                    {
                        AnswerId = ao.Id,
                        OptionLabel = ao.OptionLabel,
                        OptionText = ao.OptionText
                        // Không gửi IsCorrect cho client!
                    }).ToList()
                };

                questionsList.Add(questionDto);

                // ✨ Load QuizGroupItem nếu có (TOEIC Parts 3,4,6,7)
                if (quiz.QuizGroupItemId.HasValue && quiz.QuizGroupItem != null && !quizGroupItemsMap.ContainsKey(quiz.QuizGroupItemId.Value))
                {
                    quizGroupItemsMap[quiz.QuizGroupItemId.Value] = new QuizGroupItemDto
                    {
                        Id = quiz.QuizGroupItem.Id,
                        Name = quiz.QuizGroupItem.Name,
                        AudioUrl = quiz.QuizGroupItem.AudioUrl,
                        ImageUrl = quiz.QuizGroupItem.ImageUrl,
                        PassageText = quiz.QuizGroupItem.PassageText
                    };
                }

                // Lưu đáp án đúng vào map riêng
                foreach (var ao in answerOptions)
                {
                    correctAnswersMap[ao.Id] = ao.IsCorrect;
                }

                questionNumber++;
            }

            // Generate unique 6-digit Game PIN
            string gamePin;
            do
            {
                gamePin = new Random().Next(100000, 999999).ToString();
                var existingGame = await GetGameSessionFromRedisAsync(gamePin);
                if (existingGame == null) break; // PIN is unique
            } while (true);

            var gameSessionId = Guid.NewGuid();

            // Create game session
            var gameSession = new GameSessionDto
            {
                GamePin = gamePin,
                GameSessionId = gameSessionId,
                HostUserId = dto.HostUserId,
                HostUserName = dto.HostUserName,
                QuizSetId = dto.QuizSetId,
                EventId = dto.EventId, // Lưu EventId nếu có
                Status = GameStatus.Lobby,
                Questions = questionsList,
                QuizGroupItems = quizGroupItemsMap, // ✨ Store group items for TOEIC-style questions
                CurrentQuestionIndex = 0,
                CreatedAt = DateTime.UtcNow
            };

            await SaveGameSessionToRedisAsync(gamePin, gameSession);
            await SaveCorrectAnswersToRedisAsync(gamePin, correctAnswersMap);

            _logger.LogInformation($"✅ Game created in Redis with PIN: {gamePin} by Host: {dto.HostUserName}");
            //_logger.LogInformation($"📦 Loaded {quizGroupItemsMap.Count} quiz group items for grouped questions");

            return new CreateGameResponseDto
            {
                GamePin = gamePin,
                GameSessionId = gameSessionId,
                CreatedAt = gameSession.CreatedAt
            };
        }

        public async Task<bool> HostConnectAsync(string gamePin, string connectionId)
        {
            var session = await GetGameSessionFromRedisAsync(gamePin);
            if (session == null)
                return false;

            session.HostConnectionId = connectionId;
            await SaveGameSessionToRedisAsync(gamePin, session);
            
            _logger.LogInformation($"✅ Host connected to game {gamePin}");
                return true;
            }

        // ==================== PLAYER JOIN/LEAVE ====================
        /// <summary>
        /// Player join vào lobby bằng Game PIN
        /// </summary>
        public async Task<PlayerInfo?> PlayerJoinAsync(string gamePin, string playerName, string connectionId, Guid? userId = null)
        {
            var session = await GetGameSessionFromRedisAsync(gamePin);
            if (session == null)
                return null;

            if (session.Status != GameStatus.Lobby)
                return null; // Chỉ join được khi đang ở lobby

            // Check duplicate name
            if (session.Players.Any(p => p.PlayerName.Equals(playerName, StringComparison.OrdinalIgnoreCase)))
                return null;

            var player = new PlayerInfo
            {
                ConnectionId = connectionId,
                PlayerName = playerName,
                UserId = userId, // ✨ Lưu UserId để sync điểm vào EventParticipant
                Score = 0,
                JoinedAt = DateTime.UtcNow
            };
            
            session.Players.Add(player);
            await SaveGameSessionToRedisAsync(gamePin, session);
            
            _logger.LogInformation($"✅ Player '{playerName}' (UserId: {userId?.ToString() ?? "N/A"}) joined game {gamePin}. Total players: {session.Players.Count}");

            return player;
        }

        /// <summary>
        /// Player rời lobby
        /// </summary>
        public async Task<bool> PlayerLeaveAsync(string gamePin, string connectionId)
        {
            var session = await GetGameSessionFromRedisAsync(gamePin);
            if (session == null)
                return false;

            var player = session.Players.FirstOrDefault(p => p.ConnectionId == connectionId);
            if (player == null)
                return false;

            session.Players.Remove(player);
            await SaveGameSessionToRedisAsync(gamePin, session);

            _logger.LogInformation($"Player '{player.PlayerName}' left game {gamePin}");

            return true;
        }

        /// <summary>
        /// Lấy thông tin lobby (số người chơi, v.v.)
        /// </summary>
        public async Task<GameSessionDto?> GetGameSessionAsync(string gamePin)
        {
            return await GetGameSessionFromRedisAsync(gamePin);
        }

        // ==================== START GAME ====================
        /// <summary>
        /// Host start game - chuyển sang câu hỏi đầu tiên
        /// </summary>
        public async Task<QuestionDto?> StartGameAsync(string gamePin)
        {
            var session = await GetGameSessionFromRedisAsync(gamePin);
            if (session == null)
                return null;

            if (session.Status != GameStatus.Lobby)
                return null;

            if (session.Players.Count == 0)
                return null; // Cần ít nhất 1 player

            session.Status = GameStatus.InProgress;
            session.CurrentQuestionIndex = 0;
            session.QuestionStartedAt = DateTime.UtcNow;
            session.CurrentAnswers.Clear();

            // Boss Fight mode - track game start time and initialize PlayerQuestionStartedAt for all players
            if (session.IsBossFightMode)
            {
                session.GameStartedAt = DateTime.UtcNow;
                // Initialize PlayerQuestionStartedAt for all players so first question scoring works correctly
                foreach (var player in session.Players)
                {
                    player.PlayerQuestionStartedAt = DateTime.UtcNow;
                }
            }

            var question = session.Questions[0];

            await SaveGameSessionToRedisAsync(gamePin, session);
            
            _logger.LogInformation($"✅ Game {gamePin} started with {session.Players.Count} players");

            return question;
        }

        // ==================== SUBMIT ANSWER ====================
        /// <summary>
        /// Player submit câu trả lời
        /// </summary>
        public async Task<bool> SubmitAnswerAsync(string gamePin, string connectionId, Guid questionId, Guid answerId)
        {
            _logger.LogInformation($"📥 SubmitAnswer called: GamePin={gamePin}, QuestionId={questionId}, AnswerId={answerId}");
            
            var session = await GetGameSessionFromRedisAsync(gamePin);
            if (session == null)
            {
                _logger.LogWarning($"❌ SubmitAnswer failed: Game {gamePin} not found");
                return false;
            }

            _logger.LogInformation($"📊 Game state: Status={session.Status}, CurrentQ={session.CurrentQuestionIndex}, Answers={session.CurrentAnswers.Count}");

            if (session.Status != GameStatus.InProgress)
            {
                _logger.LogWarning($"❌ SubmitAnswer failed: Game {gamePin} status is {session.Status}, not InProgress");
                return false;
            }

            if (!session.QuestionStartedAt.HasValue)
            {
                _logger.LogWarning($"❌ SubmitAnswer failed: Game {gamePin} has no QuestionStartedAt");
                return false;
            }

            var player = session.Players.FirstOrDefault(p => p.ConnectionId == connectionId);
            if (player == null)
            {
                _logger.LogWarning($"❌ SubmitAnswer failed: Player with connection {connectionId} not found in game {gamePin}. Total players: {session.Players.Count}");
                return false;
            }

            // Check nếu đã submit rồi
            if (session.CurrentAnswers.ContainsKey(connectionId))
            {
                _logger.LogWarning($"❌ SubmitAnswer failed: Player '{player.PlayerName}' already submitted answer for question {session.CurrentQuestionIndex}");
                return false;
            }

            // Calculate time spent (for scoring)
            var timeSpent = (DateTime.UtcNow - session.QuestionStartedAt.Value).TotalSeconds;
            _logger.LogInformation($"⏱️ Time spent: {timeSpent:F2}s");

            // Check đáp án đúng
            bool isCorrect = false;
            var correctMap = await GetCorrectAnswersFromRedisAsync(gamePin);
            if (correctMap != null)
            {
                isCorrect = correctMap.GetValueOrDefault(answerId, false);
            }

            // Tính điểm: 500 điểm cơ bản + bonus theo phần trăm thời gian còn lại
            // Ví dụ: 30s câu hỏi, trả lời còn 60% thời gian → 500 + 500*0.6 = 800 điểm
            // Sử dụng TimeLimit của câu hỏi nếu Host đã đặt, mặc định 30s
            int points = 0;
            if (isCorrect)
            {
                var currentQuestion = session.Questions[session.CurrentQuestionIndex];
                int maxTime = currentQuestion.TimeLimit.HasValue && currentQuestion.TimeLimit.Value > 0
                    ? currentQuestion.TimeLimit.Value
                    : 30;
                double timeRemainingRatio = Math.Max(0, 1.0 - (timeSpent / maxTime));
                points = (int)(500 + (500 * timeRemainingRatio)); // Base 500 + bonus up to 500 = max 1000
            }

            var answer = new PlayerAnswer
            {
                ConnectionId = connectionId,
                PlayerName = player.PlayerName,
                QuestionId = questionId,
                AnswerId = answerId,
                IsCorrect = isCorrect,
                PointsEarned = points,
                TimeSpent = timeSpent,
                SubmittedAt = DateTime.UtcNow
            };

            session.CurrentAnswers[connectionId] = answer;

            // Cập nhật điểm của player
            player.Score += points;
            
            // Track answer statistics
            player.TotalAnswered++; // Always increment total answered
            if (isCorrect)
            {
                player.CorrectAnswers++;
            }

            await SaveGameSessionToRedisAsync(gamePin, session);

            _logger.LogInformation($"✅ Player '{player.PlayerName}' submitted answer for question {questionId}. Correct: {isCorrect}, Points: {points}");

            return true;
        }

        /// <summary>
        /// Host đặt thời gian trả lời (giây) cho câu hỏi hiện tại
        /// </summary>
        public async Task<bool> SetTimeForCurrentQuestionAsync(string gamePin, int seconds)
        {
            if (seconds < 5) seconds = 5;
            if (seconds > 300) seconds = 300;

            var session = await GetGameSessionFromRedisAsync(gamePin);
            if (session == null)
                return false;

            if (session.Questions == null || session.Questions.Count == 0)
                return false;

            var idx = session.CurrentQuestionIndex;
            if (idx < 0 || idx >= session.Questions.Count)
                return false;

            session.Questions[idx].TimeLimit = seconds;
            await SaveGameSessionToRedisAsync(gamePin, session);

            _logger.LogInformation($"⏱️ SetTimeForCurrentQuestion: Game {gamePin}, Q#{idx + 1}, seconds={seconds}");
            return true;
        }

        // ==================== QUESTION TIMEOUT & SHOW RESULT ====================
        /// <summary>
        /// Xử lý khi hết giờ - trả về kết quả câu hỏi
        /// </summary>
        public async Task<GameAnswerResultDto?> GetQuestionResultAsync(string gamePin)
        {
            var session = await GetGameSessionFromRedisAsync(gamePin);
            if (session == null)
                return null;

            var currentQuestion = session.Questions[session.CurrentQuestionIndex];

            // Tìm đáp án đúng
            Guid correctAnswerId = Guid.Empty;
            string correctAnswerText = string.Empty;

            var correctMap = await GetCorrectAnswersFromRedisAsync(gamePin);
            if (correctMap != null)
            {
                foreach (var option in currentQuestion.AnswerOptions)
                {
                    if (correctMap.GetValueOrDefault(option.AnswerId, false))
                    {
                        correctAnswerId = option.AnswerId;
                        correctAnswerText = option.OptionText;
                        break;
                    }
                }
            }

            // Thống kê đáp án
            var answerStats = new Dictionary<Guid, int>();
            foreach (var option in currentQuestion.AnswerOptions)
            {
                answerStats[option.AnswerId] = 0;
            }

            var playerResults = new List<PlayerAnswerResult>();

            foreach (var answer in session.CurrentAnswers.Values)
            {
                if (answerStats.ContainsKey(answer.AnswerId))
                {
                    answerStats[answer.AnswerId]++;
                }

                playerResults.Add(new PlayerAnswerResult
                {
                    PlayerName = answer.PlayerName,
                    IsCorrect = answer.IsCorrect,
                    PointsEarned = answer.PointsEarned,
                    TimeSpent = answer.TimeSpent
                });
            }

            session.Status = GameStatus.ShowingResult;
            await SaveGameSessionToRedisAsync(gamePin, session);

            return new GameAnswerResultDto
            {
                QuestionId = currentQuestion.QuestionId,
                CorrectAnswerId = correctAnswerId,
                CorrectAnswerText = correctAnswerText,
                AnswerStats = answerStats,
                PlayerResults = playerResults
            };
        }

        // ==================== LEADERBOARD & NEXT QUESTION ====================
        /// <summary>
        /// Lấy leaderboard hiện tại (không thay đổi status - dùng cho realtime update)
        /// </summary>
        public async Task<LeaderboardDto?> GetCurrentLeaderboardAsync(string gamePin)
        {
            var session = await GetGameSessionFromRedisAsync(gamePin);
            if (session == null)
                return null;

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

            // ✨ KHÔNG thay đổi status - chỉ để xem realtime

            return new LeaderboardDto
            {
                Rankings = rankings,
                CurrentQuestion = session.CurrentQuestionIndex + 1,
                TotalQuestions = session.Questions.Count
            };
        }

        /// <summary>
        /// Lấy leaderboard hiện tại và chuyển status sang ShowingLeaderboard (dùng khi hiển thị giữa các câu)
        /// </summary>
        public async Task<LeaderboardDto?> GetLeaderboardAsync(string gamePin)
        {
            var session = await GetGameSessionFromRedisAsync(gamePin);
            if (session == null)
                return null;

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

            session.Status = GameStatus.ShowingLeaderboard;
            await SaveGameSessionToRedisAsync(gamePin, session);

            return new LeaderboardDto
            {
                Rankings = rankings,
                CurrentQuestion = session.CurrentQuestionIndex + 1,
                TotalQuestions = session.Questions.Count
            };
        }

        /// <summary>
        /// Host chuyển sang câu hỏi tiếp theo
        /// </summary>
        public async Task<QuestionDto?> NextQuestionAsync(string gamePin)
        {
            var session = await GetGameSessionFromRedisAsync(gamePin);
            if (session == null)
                return null;

            session.CurrentQuestionIndex++;

            // Check nếu hết câu hỏi
            if (session.CurrentQuestionIndex >= session.Questions.Count)
            {
                session.Status = GameStatus.Completed;
                await SaveGameSessionToRedisAsync(gamePin, session);
                return null; // Hết câu hỏi
            }

            // ✨ RESET STATE FOR NEW QUESTION
            session.Status = GameStatus.InProgress;
            session.QuestionStartedAt = DateTime.UtcNow;
            session.CurrentAnswers.Clear();

            var question = session.Questions[session.CurrentQuestionIndex];

            await SaveGameSessionToRedisAsync(gamePin, session);
            
            _logger.LogInformation($"✅ Game {gamePin} moved to question {session.CurrentQuestionIndex + 1}. Status={session.Status}, QuestionStartedAt={session.QuestionStartedAt}, Answers cleared");

            return question;
        }

        // ==================== GAME END ====================
        /// <summary>
        /// Lấy kết quả cuối cùng khi game kết thúc
        /// </summary>
        public async Task<FinalResultDto?> GetFinalResultAsync(string gamePin)
        {
            // Ưu tiên lấy từ session nếu còn
            var session = await GetGameSessionFromRedisAsync(gamePin);
            if (session != null)
            {
                if (session.Status == GameStatus.Completed)
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

                    var winner = rankings.FirstOrDefault();

                    var finalResult = new FinalResultDto
                    {
                        GamePin = gamePin,
                        FinalRankings = rankings,
                        Winner = winner,
                        CompletedAt = DateTime.UtcNow,
                        TotalPlayers = session.Players.Count,
                        TotalQuestions = session.Questions.Count
                    };

                    // Lưu cache final result để API EndEvent có thể dùng nếu session bị cleanup sau đó
                    await SaveFinalResultToRedisAsync(gamePin, finalResult);
                    return finalResult;
                }
            }

            // Fallback: nếu session không còn, thử lấy finalResult đã cache
            var cachedFinal = await GetCachedFinalResultFromRedisAsync(gamePin);
            return cachedFinal;
        }

        /// <summary>
        /// Cleanup game session sau khi kết thúc
        /// </summary>
        public async Task CleanupGameAsync(string gamePin)
        {
            // ✨ Xóa khỏi Redis
            await DeleteGameFromRedisAsync(gamePin);

            _logger.LogInformation($"✅ Game {gamePin} cleaned up from Redis");
        }

        // ==================== BOSS FIGHT MODE ====================
        
        /// <summary>
        /// Update lobby settings (called when mod changes settings in lobby)
        /// This stores the settings so new players joining will receive them
        /// </summary>
        public async Task<bool> UpdateLobbySettingsAsync(string gamePin, int bossMaxHP, int? timeLimitSeconds, int questionTimeLimitSeconds)
        {
            var session = await GetGameSessionFromRedisAsync(gamePin);
            if (session == null)
                return false;

            // Only update in lobby phase
            if (session.Status != GameStatus.Lobby)
                return false;

            session.BossMaxHP = bossMaxHP;
            session.BossCurrentHP = bossMaxHP;
            session.GameTimeLimitSeconds = timeLimitSeconds;
            session.QuestionTimeLimitSeconds = questionTimeLimitSeconds;

            await SaveGameSessionToRedisAsync(gamePin, session);
            
            _logger.LogInformation($"📝 Lobby settings updated for game {gamePin}. BossHP: {bossMaxHP}, TimeLimit: {timeLimitSeconds}, QuestionTime: {questionTimeLimitSeconds}s");
            return true;
        }

        /// <summary>
        /// Bật Boss Fight mode cho game session
        /// </summary>
        public async Task<bool> EnableBossFightModeAsync(string gamePin, int bossHP = 10000, int? timeLimitSeconds = null, int questionTimeLimitSeconds = 30, bool autoNextQuestion = true)
        {
            var session = await GetGameSessionFromRedisAsync(gamePin);
            if (session == null)
                return false;

            if (session.Status != GameStatus.Lobby)
                return false;

            session.IsBossFightMode = true;
            session.BossMaxHP = bossHP;
            session.BossCurrentHP = bossHP;
            session.TotalDamageDealt = 0;
            session.BossDefeated = false;
            session.GameTimeLimitSeconds = timeLimitSeconds;
            session.QuestionTimeLimitSeconds = questionTimeLimitSeconds;
            session.AutoNextQuestion = autoNextQuestion;

            await SaveGameSessionToRedisAsync(gamePin, session);
            
            _logger.LogInformation($"🎮 Boss Fight mode enabled for game {gamePin}. Boss HP: {bossHP}, Question Time: {questionTimeLimitSeconds}s");
            return true;
        }

        /// <summary>
        /// Xử lý damage khi player trả lời đúng trong Boss Fight mode
        /// </summary>
        public async Task<BossDamagedDto?> DealDamageToBossAsync(string gamePin, string connectionId, int damage)
        {
            var session = await GetGameSessionFromRedisAsync(gamePin);
            if (session == null || !session.IsBossFightMode)
                return null;

            var player = session.Players.FirstOrDefault(p => p.ConnectionId == connectionId);
            if (player == null)
                return null;

            // Update boss HP
            session.BossCurrentHP = Math.Max(0, session.BossCurrentHP - damage);
            session.TotalDamageDealt += damage;

            // Update player damage
            player.TotalDamage += damage;

            // Check if boss is defeated
            if (session.BossCurrentHP <= 0)
            {
                session.BossDefeated = true;
            }

            await SaveGameSessionToRedisAsync(gamePin, session);

            _logger.LogInformation($"⚔️ Boss damaged by {player.PlayerName}: -{damage} HP. Boss HP: {session.BossCurrentHP}/{session.BossMaxHP}");

            return new BossDamagedDto
            {
                PlayerName = player.PlayerName,
                DamageDealt = damage,
                BossCurrentHP = session.BossCurrentHP,
                BossMaxHP = session.BossMaxHP,
                TotalDamageDealt = session.TotalDamageDealt
            };
        }

        /// <summary>
        /// Lấy kết quả Boss Fight khi boss bị đánh bại
        /// </summary>
        public async Task<BossDefeatedDto?> GetBossDefeatedResultAsync(string gamePin)
        {
            var session = await GetGameSessionFromRedisAsync(gamePin);
            if (session == null || !session.IsBossFightMode)
                return null;

            var totalDamage = session.TotalDamageDealt;
            var rankings = session.Players
                .OrderByDescending(p => p.TotalDamage)
                .Select((p, index) => new PlayerDamageRanking
                {
                    PlayerName = p.PlayerName,
                    TotalDamage = p.TotalDamage,
                    CorrectAnswers = p.CorrectAnswers,
                    TotalAnswered = p.TotalAnswered,
                    Rank = index + 1,
                    DamagePercent = totalDamage > 0 ? (double)p.TotalDamage / totalDamage * 100 : 0
                })
                .ToList();

            var timeToDefeat = session.GameStartedAt.HasValue 
                ? (DateTime.UtcNow - session.GameStartedAt.Value).TotalSeconds 
                : 0;

            return new BossDefeatedDto
            {
                GamePin = gamePin,
                TotalDamageDealt = totalDamage,
                DamageRankings = rankings,
                MvpPlayer = rankings.FirstOrDefault(),
                TimeToDefeat = timeToDefeat
            };
        }

        /// <summary>
        /// Kiểm tra xem Boss Fight đã hết giờ chưa
        /// </summary>
        public async Task<bool> IsBossFightTimeExpiredAsync(string gamePin)
        {
            var session = await GetGameSessionFromRedisAsync(gamePin);
            if (session == null || !session.IsBossFightMode)
                return false;

            if (!session.GameTimeLimitSeconds.HasValue || !session.GameStartedAt.HasValue)
                return false;

            var elapsed = (DateTime.UtcNow - session.GameStartedAt.Value).TotalSeconds;
            return elapsed >= session.GameTimeLimitSeconds.Value;
        }

        /// <summary>
        /// Lấy kết quả Boss Fight khi boss thắng (hết giờ hoặc hết câu hỏi)
        /// </summary>
        public async Task<BossDefeatedDto?> GetBossFightTimeUpResultAsync(string gamePin)
        {
            var session = await GetGameSessionFromRedisAsync(gamePin);
            if (session == null || !session.IsBossFightMode)
                return null;

            var totalDamage = session.TotalDamageDealt;
            var rankings = session.Players
                .OrderByDescending(p => p.TotalDamage)
                .Select((p, index) => new PlayerDamageRanking
                {
                    PlayerName = p.PlayerName,
                    TotalDamage = p.TotalDamage,
                    CorrectAnswers = p.CorrectAnswers,
                    TotalAnswered = p.TotalAnswered,
                    Rank = index + 1,
                    DamagePercent = totalDamage > 0 ? (double)p.TotalDamage / totalDamage * 100 : 0
                })
                .ToList();

            var timeElapsed = session.GameStartedAt.HasValue 
                ? (DateTime.UtcNow - session.GameStartedAt.Value).TotalSeconds 
                : 0;

            return new BossDefeatedDto
            {
                GamePin = gamePin,
                TotalDamageDealt = totalDamage,
                DamageRankings = rankings,
                MvpPlayer = rankings.FirstOrDefault(),
                TimeToDefeat = timeElapsed,
                BossWins = true  // Flag to indicate boss won
            };
        }

        /// <summary>
        /// Get current boss state
        /// </summary>
        public async Task<BossDamagedDto?> GetBossStateAsync(string gamePin)
        {
            var session = await GetGameSessionFromRedisAsync(gamePin);
            if (session == null || !session.IsBossFightMode)
                return null;

            return new BossDamagedDto
            {
                PlayerName = "",
                DamageDealt = 0,
                BossCurrentHP = session.BossCurrentHP,
                BossMaxHP = session.BossMaxHP,
                TotalDamageDealt = session.TotalDamageDealt
            };
        }

        // ==================== CONNECTION MANAGEMENT ====================
        /// <summary>
        /// Xử lý khi player disconnect
        /// </summary>
        public async Task<PlayerInfo?> HandleDisconnectAsync(string connectionId)
        {
            // ⚠️ NOTE: Redis không hỗ trợ scan tất cả games dễ dàng
            // Giải pháp tạm: Lưu mapping connectionId -> gamePin riêng
            // Hoặc client phải gọi LeaveGame trước khi disconnect
            
            _logger.LogWarning($"HandleDisconnectAsync called for {connectionId}. Consider implementing connection tracking in Redis.");
            
            return null;
        }

        /// <summary>
        /// Tìm game PIN từ connection ID
        /// </summary>
        public async Task<string?> GetGamePinByConnectionAsync(string connectionId)
        {
            // ⚠️ NOTE: Redis không hỗ trợ scan tất cả games dễ dàng
            // Giải pháp: Lưu mapping connectionId -> gamePin riêng trong Redis
            // Ví dụ: await _cache.GetStringAsync($"connection:{connectionId}");
            
            _logger.LogWarning($"GetGamePinByConnectionAsync called for {connectionId}. Consider implementing connection tracking in Redis.");
            
            return null;
        }

        // ==================== BOSS FIGHT PER-PLAYER QUESTION FLOW ====================
        
        /// <summary>
        /// Initialize shuffled question order for a player (called when game starts)
        /// </summary>
        private List<int> GenerateShuffledQuestionOrder(int questionCount)
        {
            var order = Enumerable.Range(0, questionCount).ToList();
            var random = new Random();
            for (int i = order.Count - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                (order[i], order[j]) = (order[j], order[i]);
            }
            return order;
        }

        /// <summary>
        /// Get next question for a specific player (Boss Fight mode)
        /// Boss Fight là mini game của Event - chỉ trả lời đúng số câu hỏi trong bộ đề, không lặp lại
        /// </summary>
        public async Task<QuestionDto?> GetPlayerNextQuestionAsync(string gamePin, string connectionId)
        {
            var session = await GetGameSessionFromRedisAsync(gamePin);
            if (session == null || session.Status != GameStatus.InProgress)
                return null;

            var player = session.Players.FirstOrDefault(p => p.ConnectionId == connectionId);
            if (player == null)
                return null;

            var totalQuestions = session.Questions.Count;
            if (totalQuestions == 0)
                return null;

            // Initialize shuffled order if not set (chỉ khi chưa hết câu hỏi)
            if (player.ShuffledQuestionOrder == null || player.ShuffledQuestionOrder.Count == 0)
            {
                // ✨ Chỉ initialize nếu chưa hết câu hỏi
                if (player.CurrentQuestionIndex >= totalQuestions)
                {
                    _logger.LogInformation($"✅ Player '{player.PlayerName}' has completed all {totalQuestions} questions. No more questions available.");
                    return null;
                }
                
                player.ShuffledQuestionOrder = GenerateShuffledQuestionOrder(totalQuestions);
                player.CurrentQuestionIndex = 0;
                player.QuestionLoopCount = 0;
                player.AnsweredQuestionIds = new HashSet<Guid>();
            }

            // ✨ Boss Fight là mini game của Event - không lặp lại câu hỏi
            // Nếu đã trả lời hết câu hỏi, không trả về câu hỏi nữa
            if (player.CurrentQuestionIndex >= totalQuestions)
            {
                _logger.LogInformation($"✅ Player '{player.PlayerName}' has completed all {totalQuestions} questions. No more questions available.");
                return null;
            }

            // Get current question index from shuffled order
            var shuffledIndex = player.ShuffledQuestionOrder[player.CurrentQuestionIndex];
            var question = session.Questions[shuffledIndex];

            // Create question DTO for player
            var questionDto = new QuestionDto
            {
                QuestionId = question.QuestionId,
                QuestionText = question.QuestionText,
                ImageUrl = question.ImageUrl,
                AudioUrl = question.AudioUrl,
                AnswerOptions = question.AnswerOptions,
                QuestionNumber = player.CurrentQuestionIndex + 1,
                TotalQuestions = totalQuestions, // Boss Fight là mini game của Event - hiển thị tổng số câu hỏi
                TimeLimit = session.QuestionTimeLimitSeconds > 0 ? session.QuestionTimeLimitSeconds : (question.TimeLimit ?? 30),
                QuizGroupItemId = question.QuizGroupItemId // Include group item reference for TOEIC-style questions
            };

            // Set the time when player receives this question (for accurate time-based scoring)
            player.PlayerQuestionStartedAt = DateTime.UtcNow;

            await SaveGameSessionToRedisAsync(gamePin, session);

            _logger.LogInformation($"📋 Player '{player.PlayerName}' getting question {player.CurrentQuestionIndex + 1}/{totalQuestions}, shuffled idx: {shuffledIndex}");

            return questionDto;
        }

        /// <summary>
        /// Move player to next question (called after player submits answer)
        /// </summary>
        public async Task<bool> MovePlayerToNextQuestionAsync(string gamePin, string connectionId, Guid answeredQuestionId)
        {
            var session = await GetGameSessionFromRedisAsync(gamePin);
            if (session == null || session.Status != GameStatus.InProgress)
                return false;

            var player = session.Players.FirstOrDefault(p => p.ConnectionId == connectionId);
            if (player == null)
                return false;

            // Mark question as answered
            player.AnsweredQuestionIds ??= new HashSet<Guid>();
            player.AnsweredQuestionIds.Add(answeredQuestionId);

            // Move to next question
            player.CurrentQuestionIndex++;

            var totalQuestions = session.Questions.Count;

            // ✨ Boss Fight là mini game của Event - KHÔNG được lặp lại câu hỏi
            // Chỉ trả lời đúng số câu trong bộ đề, không infinite loop
            if (player.CurrentQuestionIndex >= totalQuestions)
            {
                _logger.LogInformation($"✅ Player '{player.PlayerName}' completed all {totalQuestions} questions. No more questions available.");
                // Không reset, để player biết đã trả lời hết
            }

            await SaveGameSessionToRedisAsync(gamePin, session);

            return true;
        }

        /// <summary>
        /// Check if all players have completed all questions and handle game end if boss not defeated
        /// </summary>
        public async Task<bool> CheckAndHandleQuestionsExhaustedAsync(string gamePin)
        {
            var session = await GetGameSessionFromRedisAsync(gamePin);
            if (session == null || !session.IsBossFightMode || session.Status != GameStatus.InProgress)
                return false;

            // Check if boss is already defeated
            if (session.BossDefeated)
                return false;

            var totalQuestions = session.Questions.Count;
            if (totalQuestions == 0)
                return false;

            // Check if all players have completed all questions
            bool allPlayersCompleted = session.Players.Count > 0 && session.Players.All(p => p.CurrentQuestionIndex >= totalQuestions);

            if (allPlayersCompleted && session.BossCurrentHP > 0)
            {
                // All questions exhausted but boss not defeated - Boss wins
                session.Status = GameStatus.Completed;
                session.BossDefeated = false; // Boss wins
                await SaveGameSessionToRedisAsync(gamePin, session);
                
                _logger.LogInformation($"🏁 All players completed all {totalQuestions} questions but boss survived (HP: {session.BossCurrentHP}/{session.BossMaxHP}). Boss wins!");
                return true;
            }

            return false;
        }

        /// <summary>
        /// Submit answer for Boss Fight per-player mode with immediate feedback
        /// Returns the answer result immediately for the player
        /// </summary>
        public async Task<PlayerAnswerResult?> SubmitBossFightAnswerAsync(string gamePin, string connectionId, Guid questionId, Guid answerId)
        {
            var session = await GetGameSessionFromRedisAsync(gamePin);
            if (session == null || session.Status != GameStatus.InProgress)
                return null;

            var player = session.Players.FirstOrDefault(p => p.ConnectionId == connectionId);
            if (player == null)
                return null;

            // ✨ VALIDATION: Kiểm tra xem player đã trả lời câu hỏi này chưa (tránh duplicate submit)
            player.AnsweredQuestionIds ??= new HashSet<Guid>();
            if (player.AnsweredQuestionIds.Contains(questionId))
            {
                _logger.LogWarning($"⚠️ Player '{player.PlayerName}' đã trả lời câu hỏi {questionId} rồi. Bỏ qua duplicate submit.");
                // Trả về kết quả hiện tại mà không tăng TotalAnswered
                return new PlayerAnswerResult
                {
                    PlayerName = player.PlayerName,
                    IsCorrect = false, // Không biết đúng/sai vì đã trả lời rồi
                    PointsEarned = 0,
                    TimeSpent = 0,
                    CorrectAnswers = player.CorrectAnswers,
                    TotalAnswered = player.TotalAnswered
                };
            }

            // Calculate time spent using player's individual question start time
            var timeSpent = player.PlayerQuestionStartedAt.HasValue 
                ? (DateTime.UtcNow - player.PlayerQuestionStartedAt.Value).TotalSeconds 
                : 0;

            // Check answer correctness - use question-specific correct answers
            bool isCorrect = false;
            string correctAnswerText = "";
            Guid correctAnswerId = Guid.Empty;
            
            // Get correct answers only for this specific question
            var correctMap = await GetCorrectAnswersForQuestionAsync(gamePin, questionId);
            if (correctMap != null)
            {
                isCorrect = correctMap.GetValueOrDefault(answerId, false);
                // Find the correct answer ID
                correctAnswerId = correctMap.FirstOrDefault(x => x.Value).Key;
            }

            // Find correct answer text
            var question = session.Questions.FirstOrDefault(q => q.QuestionId == questionId);
            if (question != null && correctAnswerId != Guid.Empty)
            {
                var correctAnswer = question.AnswerOptions.FirstOrDefault(a => a.AnswerId == correctAnswerId);
                correctAnswerText = correctAnswer?.OptionText ?? "";
            }

            // Tính điểm: 500 điểm cơ bản + bonus theo phần trăm thời gian còn lại
            // timeSpent is calculated from player's individual question start time
            int points = 0;
            if (isCorrect)
            {
                // Use the session's question time limit setting, or question-specific, or default 30
                int maxTime = session.QuestionTimeLimitSeconds > 0 ? session.QuestionTimeLimitSeconds : (question?.TimeLimit ?? 30);
                double timeRemainingRatio = Math.Max(0, 1.0 - (timeSpent / maxTime));
                points = (int)(500 + (500 * timeRemainingRatio)); // Base 500 + bonus up to 500 = max 1000
            }

            // ✨ Mark question as answered TRƯỚC KHI update stats
            player.AnsweredQuestionIds.Add(questionId);

            // Update player stats
            player.TotalAnswered++; // Track total questions answered
            player.Score += points;
            if (isCorrect)
            {
                player.CorrectAnswers++;
                player.TotalDamage += points; // In boss fight, points = damage
            }

            var totalQuestions = session.Questions.Count;
            _logger.LogInformation($"⚔️ Player '{player.PlayerName}' answered Q:{questionId}. Correct: {isCorrect}, Points: {points}, Stats: {player.CorrectAnswers}/{player.TotalAnswered} (TotalQuestions: {totalQuestions})");

            await SaveGameSessionToRedisAsync(gamePin, session);

            return new PlayerAnswerResult
            {
                PlayerName = player.PlayerName,
                IsCorrect = isCorrect,
                PointsEarned = points,
                TimeSpent = timeSpent,
                CorrectAnswers = player.CorrectAnswers,
                TotalAnswered = player.TotalAnswered
            };
        }

        /// <summary>
        /// Force end game (mod action) - ends the boss fight immediately
        /// </summary>
        public async Task<FinalResultDto?> ForceEndGameAsync(string gamePin, string reason = "Game ended by moderator")
        {
            var session = await GetGameSessionFromRedisAsync(gamePin);
            if (session == null)
                return null;

            session.Status = GameStatus.Completed;
            session.BossDefeated = false; // Boss wins if force ended

            var rankings = session.Players
                .OrderByDescending(p => p.TotalDamage > 0 ? p.TotalDamage : p.Score)
                .Select((p, index) => new PlayerScore
                {
                    PlayerName = p.PlayerName,
                    TotalScore = p.TotalDamage > 0 ? p.TotalDamage : p.Score,
                    CorrectAnswers = p.CorrectAnswers,
                    TotalAnswered = p.TotalAnswered,
                    Rank = index + 1
                })
                .ToList();

            var winner = rankings.FirstOrDefault();

            await SaveGameSessionToRedisAsync(gamePin, session);

            _logger.LogInformation($"🛑 Game {gamePin} force ended. Reason: {reason}");

            return new FinalResultDto
            {
                GamePin = gamePin,
                FinalRankings = rankings,
                Winner = winner,
                CompletedAt = DateTime.UtcNow,
                TotalQuestions = session.Questions.Count,
                IsBossFightMode = session.IsBossFightMode,
                BossDefeated = false,
                BossMaxHP = session.BossMaxHP,
                BossCurrentHP = session.BossCurrentHP,
                TotalDamageDealt = session.TotalDamageDealt,
                ForceEnded = true,
                ForceEndReason = reason
            };
        }
    }
}
