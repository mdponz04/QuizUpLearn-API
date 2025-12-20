using BusinessLogic.DTOs;
using BusinessLogic.Interfaces;
using Microsoft.AspNetCore.SignalR;
using Repository.Entities;
using Repository.Enums;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace QuizUpLearn.API.Hubs
{
    /// <summary>
    /// SignalR Hub cho game 1vs1 và Multiplayer
    /// Flow: Player1 tạo phòng → Players join → Start → Questions → Instant Results → Next → Final Result
    /// Hỗ trợ: OneVsOne (2 players) và Multiplayer (unlimited)
    /// </summary>
    public class OneVsOneHub : Hub
    {
        private readonly IOneVsOneGameService _gameService;
        private readonly IUserService _userService;
        private readonly IQuizAttemptService _quizAttemptService;
        private readonly IQuizAttemptDetailService _quizAttemptDetailService;
        private readonly ILogger<OneVsOneHub> _logger;
        private readonly IHubContext<OneVsOneHub> _hubContext;

        public OneVsOneHub(
            IOneVsOneGameService gameService, 
            IUserService userService,
            IQuizAttemptService quizAttemptService,
            IQuizAttemptDetailService quizAttemptDetailService,
            ILogger<OneVsOneHub> logger,
            IHubContext<OneVsOneHub> hubContext)
        {
            _gameService = gameService;
            _userService = userService;
            _quizAttemptService = quizAttemptService;
            _quizAttemptDetailService = quizAttemptDetailService;
            _logger = logger;
            _hubContext = hubContext;
        }

        // ==================== HELPER METHODS ====================
        /// <summary>
        /// Build ShowQuestion payload with group item data for TOEIC-style grouped questions
        /// </summary>
        private object BuildShowQuestionPayload(QuestionDto question, OneVsOneRoomDto? room)
        {
            QuizGroupItemDto? groupItem = null;
            
            // Get group item if this question belongs to a group (TOEIC Parts 3,4,6,7)
            // Parts 1, 2, 5 are standalone - don't need group display
            var toeicPart = question.ToeicPart?.ToUpperInvariant();
            var partsWithGroupContent = new[] { "PART3", "PART4", "PART6", "PART7" };
            var shouldIncludeGroup = toeicPart != null && partsWithGroupContent.Contains(toeicPart);
            
            if (shouldIncludeGroup && 
                question.QuizGroupItemId.HasValue && 
                room?.QuizGroupItems != null && 
                room.QuizGroupItems.TryGetValue(question.QuizGroupItemId.Value, out var foundGroupItem))
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
                TimeLimit = question.TimeLimit ?? 30,
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

        // ==================== PLAYERS JOIN ROOM ====================
        /// <summary>
        /// Player join vào phòng (Player2, Player3, Player4, ...)
        /// Dùng chung cho cả 1vs1 và Multiplayer
        /// </summary>
        public async Task PlayerJoin(string roomPin, string playerName)
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

                OneVsOnePlayerDto? player;
                try
                {
                    player = await _gameService.PlayerJoinAsync(roomPin, user.Id, playerName.Trim(), Context.ConnectionId);
                }
                catch (InvalidOperationException ex) when (ex.Message.StartsWith("DUPLICATE_NAME:"))
                {
                    // Extract player name from exception message
                    var duplicateName = ex.Message.Replace("DUPLICATE_NAME:", "");
                    await Clients.Caller.SendAsync("Error", $"DUPLICATE_NAME:{duplicateName}");
                    return;
                }

                if (player == null)
                {
                    await Clients.Caller.SendAsync("Error", "Failed to join room. Room not found, already started, or full.");
                    return;
                }

                // Add vào SignalR Group
                await Groups.AddToGroupAsync(Context.ConnectionId, $"Room_{roomPin}");
                
                var room = await _gameService.GetRoomAsync(roomPin);
                _logger.LogInformation($"Player '{playerName}' joined room {roomPin} ({room?.Players.Count ?? 0} players total)");

                // Gửi xác nhận cho player vừa join
                await Clients.Caller.SendAsync("PlayerJoined", new
                {
                    RoomPin = roomPin,
                    PlayerName = playerName,
                    Message = "Successfully joined the room"
                });

                // Thông báo cho tất cả trong room
                await Clients.Group($"Room_{roomPin}").SendAsync("PlayerJoinedRoom", new
                {
                    PlayerName = playerName,
                    Timestamp = DateTime.UtcNow
                });

                // Gửi room info cập nhật
                await NotifyRoomStateChangedAsync(roomPin);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in PlayerJoin for room {roomPin}");
                await Clients.Caller.SendAsync("Error", "An error occurred while joining the room");
            }
        }

        // ==================== RECONNECT DURING GAME ====================
        /// <summary>
        /// Allow player to reconnect during an active game (update ConnectionId)
        /// Called automatically by frontend when detecting connection issues during gameplay
        /// </summary>
        public async Task ReconnectToGame(string roomPin)
        {
            try
            {
                var user = await GetAuthenticatedUserAsync();
                if (user == null) return;

                var success = await _gameService.ReconnectPlayerAsync(roomPin, user.Id, Context.ConnectionId);
                if (!success)
                {
                    await Clients.Caller.SendAsync("Error", "Failed to reconnect. You may not be in this game.");
                    return;
                }

                // Re-add to SignalR Group
                await Groups.AddToGroupAsync(Context.ConnectionId, $"Room_{roomPin}");
                
                var room = await _gameService.GetRoomAsync(roomPin);
                var player = room?.Players.FirstOrDefault(p => p.UserId == user.Id);
                
                _logger.LogInformation($"✅ Player '{player?.PlayerName}' (UserId: {user.Id}) reconnected to game in room {roomPin}");

                // Send confirmation
                await Clients.Caller.SendAsync("ReconnectedToGame", new
                {
                    RoomPin = roomPin,
                    PlayerName = player?.PlayerName,
                    CurrentQuestionIndex = room?.CurrentQuestionIndex,
                    GameStatus = room?.Status.ToString(),
                    Message = "Successfully reconnected to game"
                });

                // Notify others (optional)
                await Clients.OthersInGroup($"Room_{roomPin}").SendAsync("PlayerReconnected", new
                {
                    PlayerName = player?.PlayerName,
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in ReconnectToGame for room {roomPin}");
                await Clients.Caller.SendAsync("Error", "An error occurred while reconnecting");
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

                await Task.Delay(4000);

                // Send first question with group item data (for TOEIC-style grouped questions)
                var firstQuestion = room.Questions[0];
                var questionPayload = BuildShowQuestionPayload(firstQuestion, room);
                await Clients.Group($"Room_{roomPin}").SendAsync("ShowQuestion", questionPayload);

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
                    // Chưa đủ người trả lời
                    var room = await _gameService.GetRoomAsync(roomPin);
                    var answeredCount = room?.CurrentAnswers.Count ?? 0;
                    var totalPlayers = room?.Players.Count ?? 0;
                    
                    await Clients.Caller.SendAsync("AnswerSubmitted", new
                    {
                        QuestionId = questionGuid,
                        AnswerId = answerGuid,
                        Message = $"Waiting for other players... ({answeredCount}/{totalPlayers})",
                        AnsweredCount = answeredCount,
                        TotalPlayers = totalPlayers,
                        Timestamp = DateTime.UtcNow
                    });
                    return;
                }

                _logger.LogInformation($"✅ All players answered in room {roomPin}, showing result");

                await Clients.Group($"Room_{roomPin}").SendAsync("ShowRoundResult", result);
                _logger.LogInformation($"✅ ShowRoundResult sent to all players in room {roomPin}");

                await Clients.Group($"Room_{roomPin}").SendAsync("AnswerSubmitted", new
                {
                    QuestionId = questionGuid,
                    AnswerId = answerGuid,
                    Message = "All players answered!",
                    Result = result,
                    Timestamp = DateTime.UtcNow
                });



                // Tự động chuyển câu hỏi sau 5 giây
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
                
                // 5 giây trước khi chuyển câu hỏi
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

                // Send next question with group item data (for TOEIC-style grouped questions)
                var nextQuestion = room.Questions[room.CurrentQuestionIndex];
                var questionPayload = BuildShowQuestionPayload(nextQuestion, room);
                await _hubContext.Clients.Group($"Room_{roomPin}").SendAsync("ShowQuestion", questionPayload);
                
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

                // Lưu lịch sử chơi cho tất cả players
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await SaveGameHistoryAsync(roomPin);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Error saving game history for room {roomPin}");
                    }
                });

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

        /// <summary>
        /// Lưu lịch sử chơi cho tất cả players trong room
        /// </summary>
        private async Task SaveGameHistoryAsync(string roomPin)
        {
            try
            {
                var room = await _gameService.GetRoomAsync(roomPin);
                if (room == null)
                {
                    _logger.LogWarning($"Room {roomPin} not found when saving game history");
                    return;
                }

                // Xác định AttemptType dựa trên Mode
                string attemptType = room.Mode == GameModeEnum.OneVsOne ? "1vs1" : "Multi";

                _logger.LogInformation($"💾 Saving game history for {room.Players.Count} players in room {roomPin} (Mode: {room.Mode}, AttemptType: {attemptType})");

                // Lưu lịch sử cho mỗi player
                foreach (var player in room.Players)
                {
                    try
                    {
                        // Tính toán thống kê từ AllAnswers
                        int totalQuestions = room.Questions.Count;
                        int correctAnswers = player.CorrectAnswers;
                        int wrongAnswers = totalQuestions - correctAnswers;
                        int score = player.Score;
                        decimal accuracy = totalQuestions > 0 ? (decimal)correctAnswers / totalQuestions : 0;
                        
                        // Tính tổng thời gian (từ AllAnswers)
                        int totalTimeSpent = 0;
                        foreach (var questionAnswers in room.AllAnswers.Values)
                        {
                            if (questionAnswers.TryGetValue(player.ConnectionId, out var answer))
                            {
                                totalTimeSpent += (int)Math.Round(answer.TimeSpent);
                            }
                        }

                        // Xác định IsWinner (nếu có winner và player này là winner)
                        bool? isWinner = null;
                        var rankings = room.Players
                            .OrderByDescending(p => p.Score)
                            .ThenByDescending(p => p.CorrectAnswers)
                            .ThenBy(p => p.JoinedAt)
                            .ToList();
                        
                        if (rankings.Count > 0)
                        {
                            var topPlayer = rankings[0];
                            // Nếu chỉ có 1 người có điểm cao nhất thì đó là winner
                            if (rankings.Count(p => p.Score == topPlayer.Score) == 1 && player.UserId == topPlayer.UserId)
                            {
                                isWinner = true;
                            }
                            else if (player.UserId != topPlayer.UserId)
                            {
                                isWinner = false;
                            }
                        }

                        // Tạo QuizAttempt
                        var attemptDto = new RequestQuizAttemptDto
                        {
                            UserId = player.UserId,
                            QuizSetId = room.QuizSetId,
                            AttemptType = attemptType,
                            TotalQuestions = totalQuestions,
                            CorrectAnswers = correctAnswers,
                            WrongAnswers = wrongAnswers,
                            Score = score,
                            Accuracy = accuracy,
                            TimeSpent = totalTimeSpent > 0 ? totalTimeSpent : null,
                            OpponentId = null, // Không dùng cho 1vs1/Multi
                            IsWinner = isWinner,
                            Status = "completed"
                        };

                        var createdAttempt = await _quizAttemptService.CreateAsync(attemptDto);
                        _logger.LogInformation($"✅ Created QuizAttempt {createdAttempt.Id} for player {player.PlayerName} (UserId: {player.UserId})");

                        // Tạo QuizAttemptDetail cho mỗi question
                        foreach (var question in room.Questions)
                        {
                            // Tìm answer của player cho question này
                            if (room.AllAnswers.TryGetValue(question.QuestionId, out var questionAnswers) &&
                                questionAnswers.TryGetValue(player.ConnectionId, out var answer))
                            {
                                // Player đã trả lời câu này
                                var detailDto = new RequestQuizAttemptDetailDto
                                {
                                    AttemptId = createdAttempt.Id,
                                    QuestionId = question.QuestionId,
                                    UserAnswer = answer.AnswerId.ToString(),
                                    TimeSpent = (int)Math.Round(answer.TimeSpent)
                                };

                                await _quizAttemptDetailService.CreateAsync(detailDto);
                            }
                            else
                            {
                                // Player không trả lời câu này (timeout hoặc skip)
                                var detailDto = new RequestQuizAttemptDetailDto
                                {
                                    AttemptId = createdAttempt.Id,
                                    QuestionId = question.QuestionId,
                                    UserAnswer = string.Empty, // Không có answer
                                    TimeSpent = null
                                };

                                await _quizAttemptDetailService.CreateAsync(detailDto);
                            }
                        }

                        _logger.LogInformation($"✅ Saved {room.Questions.Count} QuizAttemptDetails for player {player.PlayerName}");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Error saving game history for player {player.PlayerName} (UserId: {player.UserId}) in room {roomPin}");
                    }
                }

                _logger.LogInformation($"✅ Successfully saved game history for all players in room {roomPin}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in SaveGameHistoryAsync for room {roomPin}");
                throw;
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
            // JWT Token structure:
            // - "sub" = Account ID (primary key for authentication)
            // - "userId" = User ID (the actual User entity ID, different from Account)
            // We need the Account ID to look up the user via GetByAccountIdAsync
            
            // Note: .NET JWT handler maps "sub" claim to ClaimTypes.NameIdentifier by default
            // So we check multiple claim types to ensure compatibility
            var accountIdClaim = Context.User?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value  // Raw "sub"
                ?? Context.User?.FindFirst("sub")?.Value  // Also try raw string "sub"
                ?? Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value  // .NET mapped sub
                ?? Context.User?.FindFirst("UserId")?.Value;  // Fallback to userId claim

            if (string.IsNullOrEmpty(accountIdClaim) || !Guid.TryParse(accountIdClaim, out var accountId))
            {
                var allClaims = Context.User?.Claims.Select(c => $"{c.Type}={c.Value}") ?? Array.Empty<string>();
                _logger.LogWarning($"❌ Failed to get Account ID from JWT token. Available claims: {string.Join(", ", allClaims)}");
                await Clients.Caller.SendAsync("Error", "Invalid user authentication. Account ID not found in token.");
                return null;
            }

            _logger.LogInformation($"✅ Found Account ID: {accountId} from claim");

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
        /// Gửi thông báo cập nhật trạng thái phòng cho cả group (hỗ trợ cả 1vs1 và Multiplayer)
        /// </summary>
        private async Task NotifyRoomStateChangedAsync(string roomPin)
        {
            var room = await _gameService.GetRoomAsync(roomPin);
            if (room == null)
            {
                _logger.LogWarning($"NotifyRoomStateChanged: Room {roomPin} not found.");
                return;
            }

            // 1. Gửi RoomUpdated với danh sách tất cả players
            await Clients.Group($"Room_{roomPin}").SendAsync("RoomUpdated", new
            {
                Status = room.Status.ToString(),
                Mode = room.Mode.ToString(),
                MaxPlayers = room.MaxPlayers,
                CurrentPlayers = room.Players.Count,
                
                // ✨ NEW: Universal Players list
                Players = room.Players.Select(p => new
                {
                    PlayerName = p.PlayerName,
                    Score = p.Score,
                    IsReady = p.IsReady,
                    IsHost = p.UserId == room.Player1?.UserId
                }).ToList(),
                
                // Backward compatibility
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
            if (room.Status == OneVsOneRoomStatus.Ready)
            {
                var message = room.Mode == GameModeEnum.OneVsOne 
                    ? "Both players are ready. You can start the game now."
                    : $"{room.Players.Count} players ready. Game can start now.";

                await Clients.Group($"Room_{roomPin}").SendAsync("RoomReady", new
                {
                    RoomPin = roomPin,
                    Mode = room.Mode.ToString(),
                    PlayerCount = room.Players.Count,
                    Players = room.Players.Select(p => new
                    {
                        PlayerName = p.PlayerName,
                        Score = p.Score,
                        IsHost = p.UserId == room.Player1?.UserId
                    }).ToList(),
                    Message = message,
                    Timestamp = DateTime.UtcNow
                });
            }
        }
    }
}

