using Microsoft.AspNetCore.SignalR;
using BusinessLogic.Services;
using BusinessLogic.DTOs;
using System.Security.Claims;

namespace QuizUpLearn.API.Hubs
{
    /// <summary>
    /// Kahoot-style Quiz Game Hub
    /// Flow: Host tạo game → Players join lobby → Host start → Show questions → Show results → Leaderboard → Next/End
    /// </summary>
    public class GameHub : Hub
    {
        private readonly RealtimeGameService _gameService;
        private readonly ILogger<GameHub> _logger;

        public GameHub(RealtimeGameService gameService, ILogger<GameHub> logger)
        {
            _gameService = gameService;
            _logger = logger;
        }

        // ==================== HELPER METHODS ====================
        /// <summary>
        /// Build ShowQuestion payload with group item data for TOEIC-style grouped questions
        /// </summary>
        private object BuildShowQuestionPayload(QuestionDto question, GameSessionDto? session)
        {
            QuizGroupItemDto? groupItem = null;
            
            // Get group item if this question belongs to a group (TOEIC Parts 3,4,6,7)
            // Parts 1, 2, 5 are standalone - don't need group display
            var toeicPart = question.ToeicPart?.ToUpperInvariant();
            var partsWithGroupContent = new[] { "PART3", "PART4", "PART6", "PART7" };
            var shouldIncludeGroup = toeicPart != null && partsWithGroupContent.Contains(toeicPart);
            
            if (shouldIncludeGroup && 
                question.QuizGroupItemId.HasValue && 
                session?.QuizGroupItems != null && 
                session.QuizGroupItems.TryGetValue(question.QuizGroupItemId.Value, out var foundGroupItem))
            {
                groupItem = foundGroupItem;
            }

            return new
            {
                // Question data
                QuestionId = question.QuestionId,
                QuestionText = question.QuestionText,
                ImageUrl = question.ImageUrl,
                AudioUrl = question.AudioUrl,
                AnswerOptions = question.AnswerOptions,
                QuestionNumber = question.QuestionNumber,
                TotalQuestions = question.TotalQuestions,
                TimeLimit = question.TimeLimit ?? session?.QuestionTimeLimitSeconds ?? 30,
                QuizGroupItemId = question.QuizGroupItemId,
                ToeicPart = question.ToeicPart, // Include TOEIC Part for frontend logic
                
                // Group item data (for TOEIC-style grouped questions with shared passage/audio/image)
                // Only included for Parts 3, 4, 6, 7
                GroupItem = groupItem != null ? new
                {
                    Id = groupItem.Id,
                    AudioUrl = groupItem.AudioUrl,
                    ImageUrl = groupItem.ImageUrl,
                    PassageText = groupItem.PassageText
                } : null
            };
        }

        // ==================== CONNECTION LIFECYCLE ====================
        public override async Task OnConnectedAsync()
        {
            _logger.LogInformation($"Client connected: {Context.ConnectionId}");
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            try
            {
                // Tìm xem connection này thuộc game nào
                var gamePin = await _gameService.GetGamePinByConnectionAsync(Context.ConnectionId);
                if (gamePin != null)
                {
                    var player = await _gameService.HandleDisconnectAsync(Context.ConnectionId);
                    if (player != null)
                    {
                        // Thông báo cho Host
                        await Clients.Group($"Game_{gamePin}").SendAsync("PlayerDisconnected", new
                        {
                            PlayerName = player.PlayerName,
                            ConnectionId = Context.ConnectionId,
                            Timestamp = DateTime.UtcNow
                        });
                    }
                }

                _logger.LogInformation($"Client disconnected: {Context.ConnectionId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in OnDisconnectedAsync");
            }

            await base.OnDisconnectedAsync(exception);
        }

        // ==================== HOST CONNECTS TO GAME ====================
        /// <summary>
        /// Host kết nối vào game sau khi tạo (qua API)
        /// </summary>
        public async Task HostConnect(string gamePin)
        {
            try
            {
                var success = await _gameService.HostConnectAsync(gamePin, Context.ConnectionId);
                if (!success)
                {
                    await Clients.Caller.SendAsync("Error", "Game not found");
                    return;
                }

                // Add Host vào SignalR Group
                await Groups.AddToGroupAsync(Context.ConnectionId, $"Game_{gamePin}");

                _logger.LogInformation($"Host connected to game {gamePin}");

                await Clients.Caller.SendAsync("HostConnected", new
                {
                    GamePin = gamePin,
                    Message = "Successfully connected as Host"
                });

                var session = await _gameService.GetGameSessionAsync(gamePin);
                if (session != null)
                {
                    await Clients.Caller.SendAsync("LobbyUpdated", new
                    {
                        TotalPlayers = session.Players.Count,
                        Players = session.Players.Select(p => new
                        {
                            PlayerName = p.PlayerName,
                            Score = p.Score
                        }).ToList()
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in HostConnect for game {gamePin}");
                await Clients.Caller.SendAsync("Error", "Failed to connect as Host");
            }
        }

        // ==================== LOBBY (WAITING ROOM) ====================
        /// <summary>
        /// Player join vào lobby bằng Game PIN
        /// </summary>
        public async Task JoinGame(string gamePin, string playerName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(playerName))
                {
                    await Clients.Caller.SendAsync("Error", "Player name is required");
                    return;
                }

                var player = await _gameService.PlayerJoinAsync(gamePin, playerName.Trim(), Context.ConnectionId);
                if (player == null)
                {
                    await Clients.Caller.SendAsync("Error", "Failed to join game. Game not found, already started, or name taken.");
                    return;
                }

                // Add Player vào SignalR Group
                await Groups.AddToGroupAsync(Context.ConnectionId, $"Game_{gamePin}");

                _logger.LogInformation($"Player '{playerName}' joined game {gamePin}");

                // Gửi cho Player xác nhận đã join
                await Clients.Caller.SendAsync("JoinedGame", new
                {
                    GamePin = gamePin,
                    PlayerName = playerName,
                    Message = "Successfully joined the game"
                });

                // Thông báo cho tất cả (kể cả Host) có người mới join
                await Clients.Group($"Game_{gamePin}").SendAsync("PlayerJoined", player);

                // Gửi lobby info cập nhật
                var session = await _gameService.GetGameSessionAsync(gamePin);
                if (session != null)
                {
                    await Clients.Group($"Game_{gamePin}").SendAsync("LobbyUpdated", new
                    {
                        TotalPlayers = session.Players.Count,
                        Players = session.Players.Select(p => new
                        {
                            PlayerName = p.PlayerName,
                            Score = p.Score
                        }).ToList()
                    });

                    // Send current Boss Fight settings to the new player if available
                    _logger.LogInformation($"🔍 JoinGame - Checking settings for {gamePin}: IsBossFightMode={session.IsBossFightMode}, BossMaxHP={session.BossMaxHP}, BossCurrentHP={session.BossCurrentHP}");
                    
                    if (session.IsBossFightMode || session.BossMaxHP > 0)
                    {
                        var settingsToSend = new
                        {
                            GamePin = gamePin,
                            BossMaxHP = session.BossMaxHP > 0 ? session.BossMaxHP : 10000,
                            BossCurrentHP = session.BossCurrentHP > 0 ? session.BossCurrentHP : session.BossMaxHP,
                            TimeLimitSeconds = session.GameTimeLimitSeconds,
                            QuestionTimeLimitSeconds = session.QuestionTimeLimitSeconds > 0 ? session.QuestionTimeLimitSeconds : 30
                        };
                        
                        _logger.LogInformation($"📤 Sending LobbySettingsUpdated to new player: BossMaxHP={settingsToSend.BossMaxHP}, BossCurrentHP={settingsToSend.BossCurrentHP}");
                        await Clients.Caller.SendAsync("LobbySettingsUpdated", settingsToSend);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in JoinGame for game {gamePin}");
                await Clients.Caller.SendAsync("Error", "An error occurred while joining the game");
            }
        }

        /// <summary>
        /// Player rời lobby (trước khi game start)
        /// </summary>
        public async Task LeaveGame(string gamePin)
        {
            try
            {
                var success = await _gameService.PlayerLeaveAsync(gamePin, Context.ConnectionId);
                if (success)
                {
                    await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Game_{gamePin}");

                    await Clients.Group($"Game_{gamePin}").SendAsync("PlayerLeft", new
                    {
                        ConnectionId = Context.ConnectionId,
                        Timestamp = DateTime.UtcNow
                    });

                    // Gửi lobby info cập nhật
                    var session = await _gameService.GetGameSessionAsync(gamePin);
                    if (session != null)
                    {
                        await Clients.Group($"Game_{gamePin}").SendAsync("LobbyUpdated", new
                        {
                            TotalPlayers = session.Players.Count,
                            Players = session.Players.Select(p => new
                            {
                                PlayerName = p.PlayerName,
                                Score = p.Score
                            }).ToList()
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in LeaveGame for game {gamePin}");
            }
        }

        // ==================== START GAME ====================
        /// <summary>
        /// Host bắt đầu game (chỉ Host mới gọi được)
        /// </summary>
        public async Task StartGame(string gamePin)
        {
            try
            {
                var question = await _gameService.StartGameAsync(gamePin);
                if (question == null)
                {
                    await Clients.Caller.SendAsync("Error", "Failed to start game");
                    return;
                }

                // Get session to check if Boss Fight mode
                var session = await _gameService.GetGameSessionAsync(gamePin);

                _logger.LogInformation($"Game {gamePin} started. Boss Fight Mode: {session?.IsBossFightMode}");

                // Gửi tín hiệu "GameStarted" cho tất cả
                await Clients.Group($"Game_{gamePin}").SendAsync("GameStarted", new
                {
                    GamePin = gamePin,
                    TotalQuestions = question.TotalQuestions,
                    Timestamp = DateTime.UtcNow,
                    // Boss Fight mode info
                    IsBossFightMode = session?.IsBossFightMode ?? false,
                    BossMaxHP = session?.BossMaxHP ?? 0,
                    BossCurrentHP = session?.BossCurrentHP ?? 0,
                    GameTimeLimitSeconds = session?.GameTimeLimitSeconds,
                    AutoNextQuestion = session?.AutoNextQuestion ?? false
                });

                // Đợi 3 giây (countdown) rồi gửi câu hỏi đầu tiên
                await Task.Delay(3000);

                // Send question with group item data (for TOEIC-style grouped questions)
                var questionPayload = BuildShowQuestionPayload(question, session);
                await Clients.Group($"Game_{gamePin}").SendAsync("ShowQuestion", questionPayload);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in StartGame for game {gamePin}");
                await Clients.Caller.SendAsync("Error", "An error occurred while starting the game");
            }
        }

        /// <summary>
        /// Host đặt thời gian (giây) cho câu hỏi hiện tại. FE nên gọi trước khi ShowQuestion hoặc ngay khi hiển thị.
        /// </summary>
        public async Task SetCurrentQuestionTime(string gamePin, int seconds)
        {
            try
            {
                var session = await _gameService.GetGameSessionAsync(gamePin);
                if (session == null)
                {
                    await Clients.Caller.SendAsync("Error", "Game not found");
                    return;
                }

                // Chỉ cho phép Host đặt thời gian
                if (!string.Equals(session.HostConnectionId, Context.ConnectionId, StringComparison.OrdinalIgnoreCase))
                {
                    await Clients.Caller.SendAsync("Error", "Only host can set time");
                    return;
                }

                var ok = await _gameService.SetTimeForCurrentQuestionAsync(gamePin, seconds);
                if (!ok)
                {
                    await Clients.Caller.SendAsync("Error", "Failed to set time for current question");
                    return;
                }

                // Phát broadcast để FE cập nhật đồng hồ
                await Clients.Group($"Game_{gamePin}").SendAsync("QuestionTimeUpdated", new
                {
                    QuestionIndex = session.CurrentQuestionIndex + 1,
                    Seconds = Math.Clamp(seconds, 5, 300)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in SetCurrentQuestionTime for game {gamePin}");
                await Clients.Caller.SendAsync("Error", "An error occurred while setting time");
            }
        }

        // ==================== SUBMIT ANSWER ====================
        /// <summary>
        /// Player submit câu trả lời
        /// </summary>
        public async Task SubmitAnswer(string gamePin, string questionId, string answerId)
        {
            try
            {
                if (!Guid.TryParse(questionId, out var questionGuid) || !Guid.TryParse(answerId, out var answerGuid))
                {
                    await Clients.Caller.SendAsync("Error", "Invalid question or answer ID");
                    return;
                }

                var success = await _gameService.SubmitAnswerAsync(gamePin, Context.ConnectionId, questionGuid, answerGuid);
                if (!success)
                {
                    await Clients.Caller.SendAsync("Error", "Failed to submit answer. Time may have expired or already answered.");
                    return;
                }

                // Gửi xác nhận cho player
                await Clients.Caller.SendAsync("AnswerSubmitted", new
                {
                    QuestionId = questionGuid,
                    AnswerId = answerGuid,
                    Timestamp = DateTime.UtcNow
                });

                // Thông báo cho Host số người đã submit
                var session = await _gameService.GetGameSessionAsync(gamePin);
                if (session != null)
                {
                    await Clients.Group($"Game_{gamePin}").SendAsync("AnswerCount", new
                    {
                        Submitted = session.CurrentAnswers.Count,
                        Total = session.Players.Count
                    });

                    // ✨ Gửi cập nhật điểm riêng cho player vừa submit (chỉ gửi cho người đó)
                    var justAnswered = session.Players.FirstOrDefault(p => p.ConnectionId == Context.ConnectionId);
                    if (justAnswered != null)
                    {
                        await Clients.Caller.SendAsync("PlayerScoreUpdated", new
                        {
                            PlayerName = justAnswered.PlayerName,
                            Score = justAnswered.Score
                        });

                        // ==================== BOSS FIGHT MODE ====================
                        if (session.IsBossFightMode)
                        {
                            // Get the answer to check if it was correct
                            var playerAnswer = session.CurrentAnswers.GetValueOrDefault(Context.ConnectionId);
                            if (playerAnswer != null && playerAnswer.IsCorrect)
                            {
                                // Score = Damage in boss fight mode
                                var damage = playerAnswer.PointsEarned;
                                var bossDamageResult = await _gameService.DealDamageToBossAsync(gamePin, Context.ConnectionId, damage);
                                
                                if (bossDamageResult != null)
                                {
                                    // Broadcast boss damage to all players
                                    await Clients.Group($"Game_{gamePin}").SendAsync("BossDamaged", bossDamageResult);

                                    // Check if boss is defeated
                                    if (bossDamageResult.BossCurrentHP <= 0)
                                    {
                                        var bossDefeatedResult = await _gameService.GetBossDefeatedResultAsync(gamePin);
                                        if (bossDefeatedResult != null)
                                        {
                                            await Clients.Group($"Game_{gamePin}").SendAsync("BossDefeated", bossDefeatedResult);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in SubmitAnswer for game {gamePin}");
                await Clients.Caller.SendAsync("Error", "An error occurred while submitting answer");
            }
        }

        // ==================== SHOW RESULT (Frontend-triggered) ====================
        /// <summary>
        /// Host trigger show result (được gọi từ frontend khi hết giờ)
        /// </summary>
        public async Task ShowQuestionResult(string gamePin)
        {
            try
            {
                var result = await _gameService.GetQuestionResultAsync(gamePin);
                if (result == null)
                {
                    _logger.LogWarning($"Failed to get question result for game {gamePin}");
                    await Clients.Caller.SendAsync("Error", "Failed to get question result");
                    return;
                }

                _logger.LogInformation($"Showing result for game {gamePin}");

                // Gửi kết quả cho tất cả
                await Clients.Group($"Game_{gamePin}").SendAsync("ShowAnswerResult", result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in ShowQuestionResult for game {gamePin}");
                await Clients.Caller.SendAsync("Error", "An error occurred while showing result");
            }
        }

        // ==================== NEXT QUESTION ====================
        /// <summary>
        /// Host chuyển sang câu tiếp theo (sau khi xem kết quả)
        /// </summary>
        public async Task NextQuestion(string gamePin)
        {
            try
            {
                // Hiển thị leaderboard trước
                var leaderboard = await _gameService.GetLeaderboardAsync(gamePin);
                if (leaderboard == null)
                {
                    await Clients.Caller.SendAsync("Error", "Failed to get leaderboard");
                    return;
                }

                await Clients.Group($"Game_{gamePin}").SendAsync("ShowLeaderboard", leaderboard);

                // Đợi 5 giây để xem leaderboard
                await Task.Delay(5000);

                // Lấy câu hỏi tiếp theo
                var nextQuestion = await _gameService.NextQuestionAsync(gamePin);

                if (nextQuestion == null)
                {
                    // Hết câu hỏi → Kết thúc game
                    await EndGame(gamePin);
                    return;
                }

                _logger.LogInformation($"Game {gamePin} moved to next question");

                // Gửi câu hỏi tiếp theo with group item data (for TOEIC-style grouped questions)
                var session = await _gameService.GetGameSessionAsync(gamePin);
                var questionPayload = BuildShowQuestionPayload(nextQuestion, session);
                await Clients.Group($"Game_{gamePin}").SendAsync("ShowQuestion", questionPayload);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in NextQuestion for game {gamePin}");
                await Clients.Caller.SendAsync("Error", "An error occurred while moving to next question");
            }
        }

        // ==================== GAME END ====================
        /// <summary>
        /// Kết thúc game và hiển thị kết quả cuối cùng
        /// </summary>
        private async Task EndGame(string gamePin)
        {
            try
            {
                var finalResult = await _gameService.GetFinalResultAsync(gamePin);
                if (finalResult == null)
                {
                    _logger.LogWarning($"Failed to get final result for game {gamePin}");
                    return;
                }

                _logger.LogInformation($"Game {gamePin} ended");

                // Gửi kết quả cuối cùng cho tất cả
                await Clients.Group($"Game_{gamePin}").SendAsync("GameEnded", finalResult);

                // TODO: Lưu kết quả vào database (QuizAttempt, QuizAttemptDetail)

                // Cleanup game session sau 1 phút
                _ = Task.Run(async () =>
                {
                    await Task.Delay(60000);
                    await _gameService.CleanupGameAsync(gamePin);
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in EndGame for game {gamePin}");
            }
        }

        // ==================== HOST CANCEL GAME ====================
        /// <summary>
        /// Host hủy game (trước hoặc trong khi chơi)
        /// </summary>
        public async Task CancelGame(string gamePin)
        {
            try
            {
                await Clients.Group($"Game_{gamePin}").SendAsync("GameCancelled", new
                {
                    GamePin = gamePin,
                    Message = "The game has been cancelled by the host",
                    Timestamp = DateTime.UtcNow
                });

                await _gameService.CleanupGameAsync(gamePin);

                _logger.LogInformation($"Game {gamePin} cancelled by host");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in CancelGame for game {gamePin}");
            }
        }

        // ==================== BOSS FIGHT MODE ====================
        /// <summary>
        /// Host enables Boss Fight mode for the game
        /// </summary>
        public async Task EnableBossFightMode(string gamePin, int bossHP = 10000, int? timeLimitSeconds = null, int questionTimeLimitSeconds = 30, bool autoNextQuestion = true)
        {
            try
            {
                var session = await _gameService.GetGameSessionAsync(gamePin);
                if (session == null)
                {
                    await Clients.Caller.SendAsync("Error", "Game not found");
                    return;
                }

                // Only host can enable boss mode
                if (!string.Equals(session.HostConnectionId, Context.ConnectionId, StringComparison.OrdinalIgnoreCase))
                {
                    await Clients.Caller.SendAsync("Error", "Only host can enable Boss Fight mode");
                    return;
                }

                var success = await _gameService.EnableBossFightModeAsync(gamePin, bossHP, timeLimitSeconds, questionTimeLimitSeconds, autoNextQuestion);
                if (!success)
                {
                    await Clients.Caller.SendAsync("Error", "Failed to enable Boss Fight mode");
                    return;
                }

                _logger.LogInformation($"🎮 Boss Fight mode enabled for game {gamePin}. Boss HP: {bossHP}, Question Time: {questionTimeLimitSeconds}s");

                // Broadcast to all players that Boss Fight mode is enabled
                await Clients.Group($"Game_{gamePin}").SendAsync("BossFightModeEnabled", new
                {
                    GamePin = gamePin,
                    BossMaxHP = bossHP,
                    BossCurrentHP = bossHP,
                    TimeLimitSeconds = timeLimitSeconds,
                    QuestionTimeLimitSeconds = questionTimeLimitSeconds,
                    AutoNextQuestion = autoNextQuestion,
                    Message = "Boss Fight mode activated! Work together to defeat the boss!"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in EnableBossFightMode for game {gamePin}");
                await Clients.Caller.SendAsync("Error", "An error occurred while enabling Boss Fight mode");
            }
        }

        /// <summary>
        /// Broadcast lobby settings to all players in real-time (when mod changes settings in lobby)
        /// Also stores the settings in the session so new players can receive them
        /// </summary>
        public async Task BroadcastLobbySettings(string gamePin, int bossMaxHP, int? timeLimitSeconds, int questionTimeLimitSeconds)
        {
            try
            {
                var session = await _gameService.GetGameSessionAsync(gamePin);
                if (session == null)
                {
                    await Clients.Caller.SendAsync("Error", "Game not found");
                    return;
                }

                // Only host can broadcast settings
                if (!string.Equals(session.HostConnectionId, Context.ConnectionId, StringComparison.OrdinalIgnoreCase))
                {
                    await Clients.Caller.SendAsync("Error", "Only host can broadcast lobby settings");
                    return;
                }

                // Store the settings in the session (so new players joining will get them)
                await _gameService.UpdateLobbySettingsAsync(gamePin, bossMaxHP, timeLimitSeconds, questionTimeLimitSeconds);

                _logger.LogInformation($"📢 Broadcasting lobby settings for game {gamePin}: BossHP={bossMaxHP}, TimeLimit={timeLimitSeconds}, QuestionTime={questionTimeLimitSeconds}");

                // Broadcast to all players (except host) in the game
                await Clients.OthersInGroup($"Game_{gamePin}").SendAsync("LobbySettingsUpdated", new
                {
                    GamePin = gamePin,
                    BossMaxHP = bossMaxHP,
                    BossCurrentHP = bossMaxHP,
                    TimeLimitSeconds = timeLimitSeconds,
                    QuestionTimeLimitSeconds = questionTimeLimitSeconds
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in BroadcastLobbySettings for game {gamePin}");
            }
        }

        /// <summary>
        /// Get current boss state (HP, damage dealt, etc.)
        /// </summary>
        public async Task GetBossState(string gamePin)
        {
            try
            {
                var bossState = await _gameService.GetBossStateAsync(gamePin);
                if (bossState == null)
                {
                    await Clients.Caller.SendAsync("Error", "Game not found or Boss Fight mode not enabled");
                    return;
                }

                await Clients.Caller.SendAsync("BossState", bossState);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in GetBossState for game {gamePin}");
                await Clients.Caller.SendAsync("Error", "An error occurred while getting boss state");
            }
        }

        /// <summary>
        /// Auto move to next question for Boss Fight mode (continuous flow)
        /// </summary>
        public async Task BossFightNextQuestion(string gamePin)
        {
            try
            {
                var session = await _gameService.GetGameSessionAsync(gamePin);
                if (session == null)
                {
                    await Clients.Caller.SendAsync("Error", "Game not found");
                    return;
                }

                // Check if boss is already defeated
                if (session.BossDefeated)
                {
                    var bossDefeatedResult = await _gameService.GetBossDefeatedResultAsync(gamePin);
                    if (bossDefeatedResult != null)
                    {
                        await Clients.Group($"Game_{gamePin}").SendAsync("BossDefeated", bossDefeatedResult);
                    }
                    return;
                }

                // Check if time limit expired
                if (await _gameService.IsBossFightTimeExpiredAsync(gamePin))
                {
                    // Boss wins - time ran out
                    var timeUpResult = await _gameService.GetBossFightTimeUpResultAsync(gamePin);
                    if (timeUpResult != null)
                    {
                        await Clients.Group($"Game_{gamePin}").SendAsync("BossFightTimeUp", timeUpResult);
                    }
                    else
                    {
                        await Clients.Group($"Game_{gamePin}").SendAsync("BossFightTimeUp", new
                        {
                            GamePin = gamePin,
                            Message = "Time's up! The boss has won!",
                            BossCurrentHP = session.BossCurrentHP,
                            BossMaxHP = session.BossMaxHP,
                            TotalDamageDealt = session.TotalDamageDealt,
                            BossWins = true
                        });
                    }
                    return;
                }

                // Get next question (skip leaderboard in boss fight mode for faster pace)
                var nextQuestion = await _gameService.NextQuestionAsync(gamePin);

                if (nextQuestion == null)
                {
                    // Out of questions but boss not defeated
                    // In boss fight mode, this means boss wins
                    var questionsExhaustedResult = await _gameService.GetBossFightTimeUpResultAsync(gamePin);
                    if (questionsExhaustedResult != null)
                    {
                        await Clients.Group($"Game_{gamePin}").SendAsync("BossFightQuestionsExhausted", questionsExhaustedResult);
                    }
                    else
                    {
                        await Clients.Group($"Game_{gamePin}").SendAsync("BossFightQuestionsExhausted", new
                        {
                            GamePin = gamePin,
                            Message = "All questions answered! But the boss survived...",
                            BossCurrentHP = session.BossCurrentHP,
                            BossMaxHP = session.BossMaxHP,
                            TotalDamageDealt = session.TotalDamageDealt,
                            BossWins = true
                        });
                    }
                    return;
                }

                _logger.LogInformation($"🎮 Boss Fight - Game {gamePin} moved to next question");

                // Send next question immediately with group item data (for TOEIC-style grouped questions)
                var questionPayload = BuildShowQuestionPayload(nextQuestion, session);
                await Clients.Group($"Game_{gamePin}").SendAsync("ShowQuestion", questionPayload);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in BossFightNextQuestion for game {gamePin}");
                await Clients.Caller.SendAsync("Error", "An error occurred while moving to next question");
            }
        }

        /// <summary>
        /// Get Boss Fight damage rankings (leaderboard by damage)
        /// </summary>
        public async Task GetBossFightLeaderboard(string gamePin)
        {
            try
            {
                var session = await _gameService.GetGameSessionAsync(gamePin);
                if (session == null || !session.IsBossFightMode)
                {
                    await Clients.Caller.SendAsync("Error", "Game not found or Boss Fight mode not enabled");
                    return;
                }

                var totalDamage = session.TotalDamageDealt;
                var rankings = session.Players
                    .OrderByDescending(p => p.TotalDamage)
                    .Select((p, index) => new
                    {
                        PlayerName = p.PlayerName,
                        TotalDamage = p.TotalDamage,
                        CorrectAnswers = p.CorrectAnswers,
                        TotalAnswered = p.TotalAnswered,
                        Rank = index + 1,
                        DamagePercent = totalDamage > 0 ? (double)p.TotalDamage / totalDamage * 100 : 0
                    })
                    .ToList();

                await Clients.Group($"Game_{gamePin}").SendAsync("BossFightLeaderboard", new
                {
                    GamePin = gamePin,
                    BossCurrentHP = session.BossCurrentHP,
                    BossMaxHP = session.BossMaxHP,
                    TotalDamageDealt = totalDamage,
                    Rankings = rankings
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in GetBossFightLeaderboard for game {gamePin}");
                await Clients.Caller.SendAsync("Error", "An error occurred while getting leaderboard");
            }
        }

        /// <summary>
        /// Get realtime leaderboard for host/mod during Boss Fight mode
        /// This can be called periodically by the frontend to update the leaderboard display
        /// </summary>
        public async Task GetRealtimeLeaderboard(string gamePin)
        {
            try
            {
                var session = await _gameService.GetGameSessionAsync(gamePin);
                if (session == null)
                {
                    await Clients.Caller.SendAsync("Error", "Game not found");
                    return;
                }

                // Only host can get realtime leaderboard
                if (!string.Equals(session.HostConnectionId, Context.ConnectionId, StringComparison.OrdinalIgnoreCase))
                {
                    await Clients.Caller.SendAsync("Error", "Only host can get realtime leaderboard");
                    return;
                }

                if (session.IsBossFightMode)
                {
                    // Boss Fight mode: rank by damage
                    var totalDamage = session.TotalDamageDealt;
                    var rankings = session.Players
                        .OrderByDescending(p => p.TotalDamage)
                        .Select((p, index) => new
                        {
                            PlayerName = p.PlayerName,
                            Score = p.Score,
                            TotalDamage = p.TotalDamage,
                            CorrectAnswers = p.CorrectAnswers,
                            TotalAnswered = p.TotalAnswered,
                            Rank = index + 1,
                            DamagePercent = totalDamage > 0 ? (double)p.TotalDamage / totalDamage * 100 : 0
                        })
                        .ToList();

                    await Clients.Caller.SendAsync("RealtimeLeaderboard", new
                    {
                        GamePin = gamePin,
                        IsBossFightMode = true,
                        BossCurrentHP = session.BossCurrentHP,
                        BossMaxHP = session.BossMaxHP,
                        TotalDamageDealt = totalDamage,
                        CurrentQuestion = session.CurrentQuestionIndex + 1,
                        TotalQuestions = session.Questions.Count,
                        TotalPlayers = session.Players.Count,
                        Rankings = rankings
                    });
                }
                else
                {
                    // Normal mode: rank by score
                    var rankings = session.Players
                        .OrderByDescending(p => p.Score)
                        .Select((p, index) => new
                        {
                            PlayerName = p.PlayerName,
                            Score = p.Score,
                            CorrectAnswers = p.CorrectAnswers,
                            Rank = index + 1
                        })
                        .ToList();

                    await Clients.Caller.SendAsync("RealtimeLeaderboard", new
                    {
                        GamePin = gamePin,
                        IsBossFightMode = false,
                        CurrentQuestion = session.CurrentQuestionIndex + 1,
                        TotalQuestions = session.Questions.Count,
                        TotalPlayers = session.Players.Count,
                        Rankings = rankings
                    });
                }

                _logger.LogDebug($"📊 Realtime leaderboard sent for game {gamePin}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in GetRealtimeLeaderboard for game {gamePin}");
                await Clients.Caller.SendAsync("Error", "An error occurred while getting realtime leaderboard");
            }
        }

        // ==================== BOSS FIGHT PER-PLAYER FLOW ====================
        
        /// <summary>
        /// Player requests their next question (Boss Fight infinite loop mode)
        /// Each player progresses independently
        /// </summary>
        public async Task GetPlayerNextQuestion(string gamePin)
        {
            try
            {
                var question = await _gameService.GetPlayerNextQuestionAsync(gamePin, Context.ConnectionId);
                if (question == null)
                {
                    await Clients.Caller.SendAsync("Error", "No question available");
                    return;
                }

                // Get session for group item lookup
                var session = await _gameService.GetGameSessionAsync(gamePin);
                var questionPayload = BuildShowQuestionPayload(question, session);

                await Clients.Caller.SendAsync("PlayerQuestion", questionPayload);
                
                _logger.LogInformation($"📋 Sent question {question.QuestionNumber} to player {Context.ConnectionId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in GetPlayerNextQuestion for game {gamePin}");
                await Clients.Caller.SendAsync("Error", "An error occurred while getting next question");
            }
        }

        /// <summary>
        /// Player submits answer for Boss Fight mode with immediate feedback
        /// Automatically moves to next question after submission
        /// </summary>
        public async Task SubmitBossFightAnswer(string gamePin, string questionId, string answerId)
        {
            try
            {
                if (!Guid.TryParse(questionId, out var questionGuid) || !Guid.TryParse(answerId, out var answerGuid))
                {
                    await Clients.Caller.SendAsync("Error", "Invalid question or answer ID");
                    return;
                }

                // Submit answer and get immediate result
                var result = await _gameService.SubmitBossFightAnswerAsync(gamePin, Context.ConnectionId, questionGuid, answerGuid);
                if (result == null)
                {
                    await Clients.Caller.SendAsync("Error", "Failed to submit answer");
                    return;
                }

                // Send immediate feedback to player
                var session = await _gameService.GetGameSessionAsync(gamePin);
                var question = session?.Questions.FirstOrDefault(q => q.QuestionId == questionGuid);
                var correctMap = await _gameService.GetCorrectAnswersForQuestionAsync(gamePin, questionGuid);
                
                string correctAnswerText = "";
                Guid correctAnswerId = Guid.Empty;
                if (question != null && correctMap != null)
                {
                    var correctEntry = correctMap.FirstOrDefault(x => x.Value);
                    correctAnswerId = correctEntry.Key;
                    correctAnswerText = question.AnswerOptions.FirstOrDefault(a => a.AnswerId == correctAnswerId)?.OptionText ?? "";
                }

                await Clients.Caller.SendAsync("BossFightAnswerResult", new
                {
                    QuestionId = questionId,
                    IsCorrect = result.IsCorrect,
                    PointsEarned = result.PointsEarned,
                    TimeSpent = result.TimeSpent,
                    CorrectAnswerId = correctAnswerId,
                    CorrectAnswerText = correctAnswerText,
                    CorrectAnswers = result.CorrectAnswers,
                    TotalAnswered = result.TotalAnswered
                });

                // If correct, deal damage to boss
                if (result.IsCorrect && session != null)
                {
                    var bossDamageResult = await _gameService.DealDamageToBossAsync(gamePin, Context.ConnectionId, result.PointsEarned);
                    if (bossDamageResult != null)
                    {
                        // Broadcast boss damage to all players
                        await Clients.Group($"Game_{gamePin}").SendAsync("BossDamaged", bossDamageResult);

                        // Check if boss is defeated
                        if (bossDamageResult.BossCurrentHP <= 0)
                        {
                            var bossDefeatedResult = await _gameService.GetBossDefeatedResultAsync(gamePin);
                            if (bossDefeatedResult != null)
                            {
                                await Clients.Group($"Game_{gamePin}").SendAsync("BossDefeated", bossDefeatedResult);
                                return; // Game ended
                            }
                        }
                    }
                }

                // Move player to next question
                await _gameService.MovePlayerToNextQuestionAsync(gamePin, Context.ConnectionId, questionGuid);

                _logger.LogInformation($"⚔️ Player submitted boss fight answer. Correct: {result.IsCorrect}, Points: {result.PointsEarned}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in SubmitBossFightAnswer for game {gamePin}");
                await Clients.Caller.SendAsync("Error", "An error occurred while submitting answer");
            }
        }

        /// <summary>
        /// Host/Mod force ends the game immediately
        /// All players see final result with notification
        /// </summary>
        public async Task ForceEndGame(string gamePin, string reason = "Game ended by moderator")
        {
            try
            {
                var session = await _gameService.GetGameSessionAsync(gamePin);
                if (session == null)
                {
                    await Clients.Caller.SendAsync("Error", "Game not found");
                    return;
                }

                // Only host can force end
                if (!string.Equals(session.HostConnectionId, Context.ConnectionId, StringComparison.OrdinalIgnoreCase))
                {
                    await Clients.Caller.SendAsync("Error", "Only host can force end the game");
                    return;
                }

                var finalResult = await _gameService.ForceEndGameAsync(gamePin, reason);
                if (finalResult == null)
                {
                    await Clients.Caller.SendAsync("Error", "Failed to end game");
                    return;
                }

                // Broadcast to all players
                await Clients.Group($"Game_{gamePin}").SendAsync("GameForceEnded", new
                {
                    GamePin = gamePin,
                    Message = reason,
                    FinalRankings = finalResult.FinalRankings,
                    Winner = finalResult.Winner,
                    IsBossFightMode = finalResult.IsBossFightMode,
                    BossDefeated = finalResult.BossDefeated,
                    BossMaxHP = finalResult.BossMaxHP,
                    BossCurrentHP = finalResult.BossCurrentHP,
                    TotalDamageDealt = finalResult.TotalDamageDealt,
                    CompletedAt = finalResult.CompletedAt
                });

                _logger.LogInformation($"🛑 Game {gamePin} force ended by host. Reason: {reason}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in ForceEndGame for game {gamePin}");
                await Clients.Caller.SendAsync("Error", "An error occurred while ending game");
            }
        }
    }
}
