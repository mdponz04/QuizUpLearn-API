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

                _logger.LogInformation($"Game {gamePin} started");

                // Gửi tín hiệu "GameStarted" cho tất cả
                await Clients.Group($"Game_{gamePin}").SendAsync("GameStarted", new
                {
                    GamePin = gamePin,
                    TotalQuestions = question.TotalQuestions,
                    Timestamp = DateTime.UtcNow
                });

                // Đợi 3 giây (countdown) rồi gửi câu hỏi đầu tiên
                await Task.Delay(3000);

                await Clients.Group($"Game_{gamePin}").SendAsync("ShowQuestion", question);
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

                // Gửi câu hỏi tiếp theo
                await Clients.Group($"Game_{gamePin}").SendAsync("ShowQuestion", nextQuestion);
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
    }
}
