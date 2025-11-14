using BusinessLogic.DTOs;
using BusinessLogic.Interfaces;
using Microsoft.AspNetCore.SignalR;
using Repository.Entities;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace QuizUpLearn.API.Hubs
{
    /// <summary>
    /// SignalR Hub cho game 1vs1
    /// Flow: Player1 tạo phòng → Player2 join → Start → Questions → Instant Results → Next → Final Result
    /// </summary>
    public class OneVsOneHub : Hub
    {
        private readonly IOneVsOneGameService _gameService;
        private readonly IUserService _userService;
        private readonly ILogger<OneVsOneHub> _logger;
        private readonly IHubContext<OneVsOneHub> _hubContext;

        public OneVsOneHub(
            IOneVsOneGameService gameService, 
            IUserService userService,
            ILogger<OneVsOneHub> logger,
            IHubContext<OneVsOneHub> hubContext)
        {
            _gameService = gameService;
            _userService = userService;
            _logger = logger;
            _hubContext = hubContext;
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
                var roomPin = await _gameService.GetRoomPinByConnectionAsync(Context.ConnectionId);
                if (roomPin != null)
                {
                    var room = await _gameService.GetRoomAsync(roomPin);
                    if (room != null)
                    {
                        await _gameService.PlayerLeaveAsync(roomPin, Context.ConnectionId);
                        
                        // Thông báo cho player còn lại
                        await Clients.Group($"Room_{roomPin}").SendAsync("PlayerDisconnected", new
                        {
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

        // ==================== PLAYER1 CREATES ROOM ====================
        public async Task Player1Connect(string roomPin)
        {
            try
            {
                // Lấy user đã xác thực
                var user = await GetAuthenticatedUserAsync();
                if (user == null) return; 

                var success = await _gameService.PlayerConnectAsync(roomPin, user.Id, Context.ConnectionId);
                if (!success)
                {
                    await Clients.Caller.SendAsync("Error", "Room not found or you are not the room creator");
                    return;
                }

                // Add vào SignalR Group
                await Groups.AddToGroupAsync(Context.ConnectionId, $"Room_{roomPin}");
                _logger.LogInformation($"Player1 connected to room {roomPin}");

                await Clients.Caller.SendAsync("Player1Connected", new
                {
                    RoomPin = roomPin,
                    Message = "Successfully connected as Player1"
                });

                // Gửi trạng thái phòng hiện tại
                await NotifyRoomStateChangedAsync(roomPin);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in Player1Connect for room {roomPin}");
                await Clients.Caller.SendAsync("Error", "Failed to connect as Player1");
            }
        }

        // ==================== PLAYER2 JOINS ROOM ====================
        public async Task Player2Join(string roomPin, string playerName)
        {
            try
            {
                // Lấy user đã xác thực
                var user = await GetAuthenticatedUserAsync();
                if (user == null) return; 

                if (string.IsNullOrWhiteSpace(playerName))
                {
                    await Clients.Caller.SendAsync("Error", "Player name is required");
                    return;
                }

                var player = await _gameService.PlayerJoinAsync(roomPin, user.Id, playerName.Trim(), Context.ConnectionId);
                if (player == null)
                {
                    await Clients.Caller.SendAsync("Error", "Failed to join room. Room not found, already started, or full.");
                    return;
                }

                // Add vào SignalR Group
                await Groups.AddToGroupAsync(Context.ConnectionId, $"Room_{roomPin}");
                _logger.LogInformation($"Player2 '{playerName}' joined room {roomPin}");

                // Gửi xác nhận cho Player2
                await Clients.Caller.SendAsync("Player2Joined", new
                {
                    RoomPin = roomPin,
                    PlayerName = playerName,
                    Message = "Successfully joined the room"
                });

                // Thông báo cho cả 2 người
                await Clients.Group($"Room_{roomPin}").SendAsync("PlayerJoined", player);

                // Gửi room info cập nhật
                await NotifyRoomStateChangedAsync(roomPin);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in Player2Join for room {roomPin}");
                await Clients.Caller.SendAsync("Error", "An error occurred while joining the room");
            }
        }

        // ==================== START GAME ====================
        public async Task StartGame(string roomPin)
        {
            try
            {
                var success = await _gameService.StartGameAsync(roomPin);
                if (!success)
                {
                    await Clients.Caller.SendAsync("Error", "Failed to start game");
                    return;
                }

                var room = await _gameService.GetRoomAsync(roomPin);
                if (room == null || room.Questions.Count == 0)
                {
                    await Clients.Caller.SendAsync("Error", "Room not found or no questions");
                    return;
                }

                _logger.LogInformation($"1v1 Game started in room {roomPin}");

                // Gửi tín hiệu "GameStarted"
                await Clients.Group($"Room_{roomPin}").SendAsync("GameStarted", new
                {
                    RoomPin = roomPin,
                    TotalQuestions = room.Questions.Count,
                    Timestamp = DateTime.UtcNow
                });

                await Task.Delay(3000);

                var firstQuestion = room.Questions[0];
                await Clients.Group($"Room_{roomPin}").SendAsync("ShowQuestion", firstQuestion);

                _ = StartQuestionTimerAsync(roomPin, firstQuestion.QuestionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in StartGame for room {roomPin}");
                await Clients.Caller.SendAsync("Error", "An error occurred while starting the game");
            }
        }

        // ==================== SUBMIT ANSWER ====================
        /// <summary>
        /// Player submit câu trả lời
        /// </summary>
        public async Task SubmitAnswer(string roomPin, string questionId, string answerId)
        {
            try
            {
                if (!Guid.TryParse(questionId, out var questionGuid) || !Guid.TryParse(answerId, out var answerGuid))
                {
                    await Clients.Caller.SendAsync("Error", "Invalid question or answer ID");
                    return;
                }

                var result = await _gameService.SubmitAnswerAsync(roomPin, Context.ConnectionId, questionGuid, answerGuid);
                
                if (result == null)
                {
                    await Clients.Caller.SendAsync("AnswerSubmitted", new
                    {
                        QuestionId = questionGuid,
                        AnswerId = answerGuid,
                        Message = "Waiting for opponent...",
                        Timestamp = DateTime.UtcNow
                    });
                    return;
                }

                _logger.LogInformation($"✅ Both players answered in room {roomPin}, showing result");

                await Clients.Group($"Room_{roomPin}").SendAsync("ShowRoundResult", result);
                _logger.LogInformation($"✅ ShowRoundResult sent to all players in room {roomPin}");

                await Clients.Group($"Room_{roomPin}").SendAsync("AnswerSubmitted", new
                {
                    QuestionId = questionGuid,
                    AnswerId = answerGuid,
                    Message = "Both players answered!",
                    Result = result,
                    Timestamp = DateTime.UtcNow
                });



                // ✨ Tự động chuyển câu hỏi sau 5 giây
                _logger.LogInformation($"🔄 Starting AutoNextQuestionAsync for room {roomPin} (will execute in 5 seconds)");
                
                _ = AutoNextQuestionAsync(roomPin);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in SubmitAnswer for room {roomPin}");
                await Clients.Caller.SendAsync("Error", "An error occurred while submitting answer");
            }
        }

        // ==================== NEXT QUESTION ====================
        /// <summary>
        /// Chuyển sang câu tiếp theo (tự động hoặc manual)
        /// </summary>
        private async Task AutoNextQuestionAsync(string roomPin)
        {
            try
            {
                _logger.LogInformation($"🔄 AutoNextQuestionAsync started for room {roomPin} - Waiting 5 seconds...");
                
                // Đợi 5 giây trước khi chuyển câu hỏi
                await Task.Delay(5000);

                _logger.LogInformation($"🔄 AutoNextQuestionAsync: 5s delay completed, calling NextQuestionAsync for room {roomPin}");

                var success = await _gameService.NextQuestionAsync(roomPin);
                if (!success)
                {
                    _logger.LogInformation($"🔄 AutoNextQuestionAsync: No more questions, ending game for room {roomPin}");
                    // Hết câu hỏi → Kết thúc game
                    await EndGame(roomPin);
                    return;
                }

                var room = await _gameService.GetRoomAsync(roomPin);
                if (room == null)
                {
                    _logger.LogWarning($"❌ Room {roomPin} not found in AutoNextQuestion");
                    return;
                }

                _logger.LogInformation($"✅ Room {roomPin} auto-moving to next question (Index: {room.CurrentQuestionIndex + 1}/{room.Questions.Count})");

                var nextQuestion = room.Questions[room.CurrentQuestionIndex];
                await _hubContext.Clients.Group($"Room_{roomPin}").SendAsync("ShowQuestion", nextQuestion);
                
                _logger.LogInformation($"✅ ShowQuestion sent for room {roomPin}, question {room.CurrentQuestionIndex + 1}");

                _ = StartQuestionTimerAsync(roomPin, nextQuestion.QuestionId);
                _logger.LogInformation($"✅ Timer 30s started for room {roomPin}, question {nextQuestion.QuestionId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Error in AutoNextQuestion for room {roomPin}: {ex.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// Timer 30 giây cho mỗi câu hỏi - tự động show result nếu chưa có
        /// </summary>
        private async Task StartQuestionTimerAsync(string roomPin, Guid questionId)
        {
            try
            {
                await Task.Delay(30000); 

                var room = await _gameService.GetRoomAsync(roomPin);
                if (room == null)
                    return;


                if (room.Status == OneVsOneRoomStatus.InProgress && 
                    room.CurrentQuestionIndex < room.Questions.Count &&
                    room.Questions[room.CurrentQuestionIndex].QuestionId == questionId)
                {
                    _logger.LogInformation($"30s timer expired for room {roomPin}, auto-showing result");

                    var result = await _gameService.GetCurrentRoundResultAsync(roomPin);
                    if (result != null)
                    {
                        await _hubContext.Clients.Group($"Room_{roomPin}").SendAsync("ShowRoundResult", result);
                        await _gameService.MarkResultShownAsync(roomPin);

                        _ = AutoNextQuestionAsync(roomPin);
                    }
                }
                else
                {
                    _logger.LogInformation($"Timer expired for room {roomPin} but result already shown or question changed. Status: {room.Status}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in StartQuestionTimer for room {roomPin}");
            }
        }

        // ==================== GAME END ====================
        /// <summary>
        /// Kết thúc game và hiển thị kết quả cuối cùng
        /// </summary>
        private async Task EndGame(string roomPin)
        {
            try
            {
                var finalResult = await _gameService.GetFinalResultAsync(roomPin);
                if (finalResult == null)
                {
                    _logger.LogWarning($"Failed to get final result for room {roomPin}");
                    return;
                }

                _logger.LogInformation($"1v1 Game ended in room {roomPin}");

                await _hubContext.Clients.Group($"Room_{roomPin}").SendAsync("GameEnded", finalResult);
                _logger.LogInformation($"✅ GameEnded sent for room {roomPin}");

                _ = Task.Run(async () =>
                {
                    await Task.Delay(60000);
                    await _gameService.CleanupRoomAsync(roomPin);
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in EndGame for room {roomPin}");
            }
        }

        // ==================== CANCEL ROOM ====================
        /// <summary>
        /// Hủy phòng (chỉ Player1 có thể hủy)
        /// </summary>
        public async Task CancelRoom(string roomPin)
        {
            try
            {
                await Clients.Group($"Room_{roomPin}").SendAsync("RoomCancelled", new
                {
                    RoomPin = roomPin,
                    Message = "The room has been cancelled",
                    Timestamp = DateTime.UtcNow
                });

                await _gameService.CleanupRoomAsync(roomPin);

                _logger.LogInformation($"Room {roomPin} cancelled");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in CancelRoom for room {roomPin}");
            }
        }
        private async Task<ResponseUserDto?> GetAuthenticatedUserAsync()
        {
            var accountIdClaim = Context.User?.FindFirst("UserId")?.Value
                ?? Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? Context.User?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

            if (string.IsNullOrEmpty(accountIdClaim) || !Guid.TryParse(accountIdClaim, out var accountId))
            {
                _logger.LogWarning($"❌ Failed to get Account ID from JWT token. Claims: {string.Join(", ", Context.User?.Claims.Select(c => $"{c.Type}={c.Value}") ?? Array.Empty<string>())}");
                await Clients.Caller.SendAsync("Error", "Invalid user authentication. Account ID not found in token.");
                return null;
            }

            var user = await _userService.GetByAccountIdAsync(accountId);
            if (user == null)
            {
                _logger.LogWarning($"❌ User not found for Account ID: {accountId}");
                await Clients.Caller.SendAsync("Error", "User not found for this account.");
                return null;
            }

            return user;
        }

        /// <summary>
        /// **HÀM MỚI:** Gửi thông báo cập nhật trạng thái phòng cho cả group.
        /// </summary>
        private async Task NotifyRoomStateChangedAsync(string roomPin)
        {
            var room = await _gameService.GetRoomAsync(roomPin);
            if (room == null)
            {
                _logger.LogWarning($"NotifyRoomStateChanged: Room {roomPin} not found.");
                return;
            }

            // 1. Gửi RoomUpdated
            await Clients.Group($"Room_{roomPin}").SendAsync("RoomUpdated", new
            {
                Status = room.Status.ToString(),
                Player1 = room.Player1 != null ? new
                {
                    PlayerName = room.Player1.PlayerName,
                    Score = room.Player1.Score,
                    IsReady = room.Player1.IsReady
                } : null,
                Player2 = room.Player2 != null ? new
                {
                    PlayerName = room.Player2.PlayerName,
                    Score = room.Player2.Score,
                    IsReady = room.Player2.IsReady
                } : null
            });

            // 2. Nếu đã sẵn sàng, gửi RoomReady
            if (room.Status == OneVsOneRoomStatus.Ready && room.Player1 != null && room.Player2 != null)
            {
                await Clients.Group($"Room_{roomPin}").SendAsync("RoomReady", new
                {
                    RoomPin = roomPin,
                    Player1 = new
                    {
                        PlayerName = room.Player1.PlayerName,
                        Score = room.Player1.Score
                    },
                    Player2 = new
                    {
                        PlayerName = room.Player2.PlayerName,
                        Score = room.Player2.Score
                    },
                    Message = "Both players are ready. You can start the game now.",
                    Timestamp = DateTime.UtcNow
                });
            }
        }
    }
}

