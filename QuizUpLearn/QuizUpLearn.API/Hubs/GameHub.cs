using Microsoft.AspNetCore.SignalR;
using BusinessLogic.Services;
using BusinessLogic.DTOs;
using System.Security.Claims;

namespace QuizUpLearn.API.Hubs
{
    public class GameHub : Hub
    {
        private readonly RealtimeGameService _gameService;
        private readonly ILogger<GameHub> _logger;

        public GameHub(RealtimeGameService gameService, ILogger<GameHub> logger)
        {
            _gameService = gameService;
            _logger = logger;
        }

        public override async Task OnConnectedAsync()
        {
            try
            {
                var userId = GetUserIdFromContext();
                if (userId.HasValue)
                {
                    await _gameService.AddConnectionAsync(Context.ConnectionId, userId.Value);
                    _logger.LogInformation($"User {userId} connected with connection {Context.ConnectionId}");
                }
                await base.OnConnectedAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in OnConnectedAsync");
                throw;
            }
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            try
            {
                var userId = await _gameService.GetUserIdByConnectionIdAsync(Context.ConnectionId);
                if (userId.HasValue)
                {
                    await _gameService.RemoveConnectionAsync(Context.ConnectionId);
                    _logger.LogInformation($"User {userId} disconnected from connection {Context.ConnectionId}");
                }
                await base.OnDisconnectedAsync(exception);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in OnDisconnectedAsync");
                throw;
            }
        }

        // Join a game room
        public async Task JoinRoom(string roomId)
        {
            try
            {
                var userId = GetUserIdFromContext();
                if (!userId.HasValue)
                {
                    await Clients.Caller.SendAsync("Error", "User not authenticated");
                    return;
                }

                var roomInfo = await _gameService.GetGameRoomInfoAsync(roomId);
                if (roomInfo == null)
                {
                    await Clients.Caller.SendAsync("Error", "Room not found");
                    return;
                }

                // Add user to SignalR group
                await Groups.AddToGroupAsync(Context.ConnectionId, roomId);

                // Join the game room
                var joinDto = new JoinGameRoomDto
                {
                    RoomId = roomId,
                    UserId = userId.Value,
                    UserName = GetUserNameFromContext()
                };

                var success = await _gameService.JoinGameRoomAsync(joinDto);
                if (success)
                {
                    // Notify all users in the room
                    await Clients.Group(roomId).SendAsync("PlayerJoined", new
                    {
                        UserId = userId.Value,
                        UserName = joinDto.UserName,
                        RoomId = roomId,
                        Timestamp = DateTime.UtcNow
                    });

                    // Send updated room info
                    var updatedRoomInfo = await _gameService.GetGameRoomInfoAsync(roomId);
                    await Clients.Group(roomId).SendAsync("RoomUpdated", updatedRoomInfo);
                }
                else
                {
                    await Clients.Caller.SendAsync("Error", "Failed to join room");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in JoinRoom");
                await Clients.Caller.SendAsync("Error", "An error occurred while joining the room");
            }
        }

        // Leave a game room
        public async Task LeaveRoom(string roomId)
        {
            try
            {
                var userId = GetUserIdFromContext();
                if (!userId.HasValue)
                {
                    await Clients.Caller.SendAsync("Error", "User not authenticated");
                    return;
                }

                // Remove user from SignalR group
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, roomId);

                // Leave the game room
                var leaveDto = new LeaveGameDto
                {
                    RoomId = roomId,
                    UserId = userId.Value
                };

                var success = await _gameService.LeaveGameRoomAsync(leaveDto);
                if (success)
                {
                    // Notify all users in the room
                    await Clients.Group(roomId).SendAsync("PlayerLeft", new
                    {
                        UserId = userId.Value,
                        RoomId = roomId,
                        Timestamp = DateTime.UtcNow
                    });

                    // Send updated room info
                    var updatedRoomInfo = await _gameService.GetGameRoomInfoAsync(roomId);
                    await Clients.Group(roomId).SendAsync("RoomUpdated", updatedRoomInfo);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in LeaveRoom");
                await Clients.Caller.SendAsync("Error", "An error occurred while leaving the room");
            }
        }

        // Start the game
        public async Task StartGame(string roomId)
        {
            try
            {
                var userId = GetUserIdFromContext();
                if (!userId.HasValue)
                {
                    await Clients.Caller.SendAsync("Error", "User not authenticated");
                    return;
                }

                var startDto = new StartGameDto
                {
                    RoomId = roomId,
                    UserId = userId.Value
                };

                var success = await _gameService.StartGameAsync(startDto);
                if (success)
                {
                    // Notify all users in the room
                    await Clients.Group(roomId).SendAsync("GameStarted", new
                    {
                        RoomId = roomId,
                        Timestamp = DateTime.UtcNow
                    });

                    // Send first question to both players
                    var hostQuestion = await _gameService.GetNextQuestionAsync(roomId, userId.Value);
                    var guestUserId = (await _gameService.GetGameRoomInfoAsync(roomId))?.GuestUserId;
                    
                    if (hostQuestion != null)
                    {
                        await Clients.Caller.SendAsync("QuestionReceived", hostQuestion);
                    }

                    if (guestUserId.HasValue)
                    {
                        var guestQuestion = await _gameService.GetNextQuestionAsync(roomId, guestUserId.Value);
                        if (guestQuestion != null)
                        {
                            var guestConnectionId = await _gameService.GetConnectionIdAsync(guestUserId.Value);
                            if (!string.IsNullOrEmpty(guestConnectionId))
                            {
                                await Clients.Client(guestConnectionId).SendAsync("QuestionReceived", guestQuestion);
                            }
                        }
                    }
                }
                else
                {
                    await Clients.Caller.SendAsync("Error", "Failed to start game");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in StartGame");
                await Clients.Caller.SendAsync("Error", "An error occurred while starting the game");
            }
        }

        // Submit an answer
        public async Task SubmitAnswer(string roomId, int questionId, int answerOptionId, double timeSpent)
        {
            try
            {
                var userId = GetUserIdFromContext();
                if (!userId.HasValue)
                {
                    await Clients.Caller.SendAsync("Error", "User not authenticated");
                    return;
                }

                var answerDto = new SubmitAnswerDto
                {
                    RoomId = roomId,
                    UserId = userId.Value,
                    QuestionId = questionId,
                    AnswerOptionId = answerOptionId,
                    TimeSpent = timeSpent
                };

                var success = await _gameService.SubmitAnswerAsync(answerDto);
                if (success)
                {
                    // Notify that an answer was submitted
                    await Clients.Group(roomId).SendAsync("AnswerSubmitted", new
                    {
                        UserId = userId.Value,
                        QuestionId = questionId,
                        RoomId = roomId,
                        Timestamp = DateTime.UtcNow
                    });

                    // Check if game is completed
                    var roomInfo = await _gameService.GetGameRoomInfoAsync(roomId);
                    if (roomInfo?.Status == GameRoomStatus.Completed)
                    {
                        var gameResult = await _gameService.GetGameResultAsync(roomId);
                        if (gameResult != null)
                        {
                            await Clients.Group(roomId).SendAsync("GameEnded", gameResult);
                        }
                    }
                }
                else
                {
                    await Clients.Caller.SendAsync("Error", "Failed to submit answer");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in SubmitAnswer");
                await Clients.Caller.SendAsync("Error", "An error occurred while submitting the answer");
            }
        }

        // Get next question
        public async Task GetNextQuestion(string roomId)
        {
            try
            {
                var userId = GetUserIdFromContext();
                if (!userId.HasValue)
                {
                    await Clients.Caller.SendAsync("Error", "User not authenticated");
                    return;
                }

                var question = await _gameService.GetNextQuestionAsync(roomId, userId.Value);
                if (question != null)
                {
                    await Clients.Caller.SendAsync("QuestionReceived", question);
                }
                else
                {
                    await Clients.Caller.SendAsync("NoMoreQuestions", new { RoomId = roomId });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetNextQuestion");
                await Clients.Caller.SendAsync("Error", "An error occurred while getting the next question");
            }
        }

        private int? GetUserIdFromContext()
        {
            var userIdClaim = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(userIdClaim, out var userId))
            {
                return userId;
            }
            return null;
        }

        private string? GetUserNameFromContext()
        {
            return Context.User?.FindFirst(ClaimTypes.Name)?.Value;
        }
    }
}
