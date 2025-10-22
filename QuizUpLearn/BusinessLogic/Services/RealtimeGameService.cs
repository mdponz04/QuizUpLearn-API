using BusinessLogic.DTOs;
using Repository.Interfaces;
using System.Collections.Concurrent;

namespace BusinessLogic.Services
{
    public class RealtimeGameService
    {
        private readonly IQuizSetRepo _quizSetRepository;
        private readonly IQuizRepo _quizRepository;
        private readonly IAnswerOptionRepo _answerOptionRepository;

        // In-memory storage for game rooms
        private static readonly ConcurrentDictionary<string, GameRoomInfoDto> _gameRooms = new();
        private static readonly ConcurrentDictionary<string, List<GameQuestionDto>> _roomQuestions = new();
        private static readonly ConcurrentDictionary<string, List<SubmitAnswerDto>> _roomAnswers = new();
        private static readonly ConcurrentDictionary<string, string> _userConnections = new();

        public RealtimeGameService(
            IQuizSetRepo quizSetRepository,
            IQuizRepo quizRepository,
            IAnswerOptionRepo answerOptionRepository)
        {
            _quizSetRepository = quizSetRepository;
            _quizRepository = quizRepository;
            _answerOptionRepository = answerOptionRepository;
        }

        // Tạo phòng game
        public async Task<string> CreateGameRoomAsync(CreateGameRoomDto createDto)
        {
            // Validate quiz set exists
            var quizSet = await _quizSetRepository.GetQuizSetByIdAsync(new Guid(createDto.QuizSetId.ToString()));
            if (quizSet == null)
                throw new ArgumentException("Quiz set not found");

            // Generate unique room ID
            var roomId = Guid.NewGuid().ToString("N")[..8].ToUpper();
            
            // Create game room
            var gameRoom = new GameRoomInfoDto
            {
                RoomId = roomId,
                HostUserId = createDto.HostUserId,
                HostUserName = createDto.HostUserName,
                QuizSetId = createDto.QuizSetId,
                TimeLimit = createDto.TimeLimit,
                Status = GameRoomStatus.Waiting,
                CreatedAt = DateTime.UtcNow
            };

            _gameRooms[roomId] = gameRoom;

            // Load questions for the room
            await LoadQuestionsForRoomAsync(roomId, createDto.QuizSetId);

            return roomId;
        }

        // Join phòng game
        public async Task<bool> JoinGameRoomAsync(JoinGameRoomDto joinDto)
        {
            if (!_gameRooms.TryGetValue(joinDto.RoomId, out var room))
                return false;

            if (room.Status != GameRoomStatus.Waiting)
                return false;

            if (room.GuestUserId.HasValue)
                return false; // Room is full

            // Update room with guest user
            room.GuestUserId = joinDto.UserId;
            room.GuestUserName = joinDto.UserName;
            _gameRooms[joinDto.RoomId] = room;

            return true;
        }

        // Leave phòng game
        public async Task<bool> LeaveGameRoomAsync(LeaveGameDto leaveDto)
        {
            if (!_gameRooms.TryGetValue(leaveDto.RoomId, out var room))
                return false;

            // If host leaves, cancel the room
            if (room.HostUserId == leaveDto.UserId)
            {
                room.Status = GameRoomStatus.Cancelled;
                _gameRooms[leaveDto.RoomId] = room;
                return true;
            }

            // If guest leaves, remove guest
            if (room.GuestUserId == leaveDto.UserId)
            {
                room.GuestUserId = null;
                room.GuestUserName = null;
                _gameRooms[leaveDto.RoomId] = room;
                return true;
            }

            return false;
        }

        // Start game
        public async Task<bool> StartGameAsync(StartGameDto startDto)
        {
            if (!_gameRooms.TryGetValue(startDto.RoomId, out var room))
                return false;

            if (room.HostUserId != startDto.UserId)
                return false; // Only host can start the game

            if (!room.GuestUserId.HasValue)
                return false; // Need a guest to start

            room.Status = GameRoomStatus.InProgress;
            _gameRooms[startDto.RoomId] = room;

            return true;
        }

        // Submit answer
        public async Task<bool> SubmitAnswerAsync(SubmitAnswerDto answerDto)
        {
            if (!_gameRooms.TryGetValue(answerDto.RoomId, out var room))
                return false;

            if (room.Status != GameRoomStatus.InProgress)
                return false;

            // Add answer to room answers
            var answers = _roomAnswers.GetOrAdd(answerDto.RoomId, new List<SubmitAnswerDto>());
            answers.Add(answerDto);

            // Check if both players have answered all questions
            var hostAnswers = answers.Count(a => a.UserId == room.HostUserId);
            var guestAnswers = answers.Count(a => a.UserId == room.GuestUserId);
            var totalQuestions = _roomQuestions.GetValueOrDefault(answerDto.RoomId, new List<GameQuestionDto>()).Count;

            if (hostAnswers == guestAnswers && hostAnswers == totalQuestions)
            {
                // Game completed
                room.Status = GameRoomStatus.Completed;
                _gameRooms[answerDto.RoomId] = room;
            }

            return true;
        }

        // Get next question
        public async Task<GameQuestionDto?> GetNextQuestionAsync(string roomId, int userId)
        {
            if (!_roomQuestions.TryGetValue(roomId, out var questions))
                return null;

            if (!_gameRooms.TryGetValue(roomId, out var room))
                return null;

            if (room.Status != GameRoomStatus.InProgress)
                return null;

            // Get current question index based on answered questions
            var answers = _roomAnswers.GetValueOrDefault(roomId, new List<SubmitAnswerDto>());
            var answeredCount = answers.Count(a => a.UserId == userId);

            if (answeredCount >= questions.Count)
                return null; // All questions answered

            var question = questions[answeredCount];
            question.QuestionNumber = answeredCount + 1;
            question.TotalQuestions = questions.Count;

            return question;
        }

        // Get game result
        public async Task<GameResultDto?> GetGameResultAsync(string roomId)
        {
            if (!_gameRooms.TryGetValue(roomId, out var room))
                return null;

            if (room.Status != GameRoomStatus.Completed)
                return null;

            var answers = _roomAnswers.GetValueOrDefault(roomId, new List<SubmitAnswerDto>());
            var questions = _roomQuestions.GetValueOrDefault(roomId, new List<GameQuestionDto>());

            // Calculate scores
            var hostScore = CalculateScore(room.HostUserId, answers, questions);
            var guestScore = CalculateScore(room.GuestUserId, answers, questions);

            var result = new GameResultDto
            {
                RoomId = roomId,
                HostUserId = room.HostUserId,
                HostUserName = room.HostUserName,
                HostScore = hostScore,
                GuestUserId = room.GuestUserId,
                GuestUserName = room.GuestUserName,
                GuestScore = guestScore,
                WinnerUserId = hostScore > guestScore ? room.HostUserId : room.GuestUserId ?? 0,
                WinnerUserName = hostScore > guestScore ? room.HostUserName : room.GuestUserName,
                CompletedAt = DateTime.UtcNow
            };

            return result;
        }

        // Get room info
        public async Task<GameRoomInfoDto?> GetGameRoomInfoAsync(string roomId)
        {
            _gameRooms.TryGetValue(roomId, out var room);
            return room;
        }

        // Get available rooms
        public async Task<List<GameRoomInfoDto>> GetAvailableRoomsAsync()
        {
            return _gameRooms.Values
                .Where(r => r.Status == GameRoomStatus.Waiting && !r.GuestUserId.HasValue)
                .OrderByDescending(r => r.CreatedAt)
                .ToList();
        }

        // Connection management
        public async Task AddConnectionAsync(string connectionId, int userId)
        {
            _userConnections[connectionId] = userId.ToString();
        }

        public async Task RemoveConnectionAsync(string connectionId)
        {
            _userConnections.TryRemove(connectionId, out _);
        }

        public async Task<string?> GetConnectionIdAsync(int userId)
        {
            var connection = _userConnections.FirstOrDefault(x => x.Value == userId.ToString());
            return connection.Key;
        }

        public async Task<int?> GetUserIdByConnectionIdAsync(string connectionId)
        {
            if (_userConnections.TryGetValue(connectionId, out var userIdStr) && 
                int.TryParse(userIdStr, out var userId))
                return userId;
            return null;
        }

        // Private methods
        private async Task LoadQuestionsForRoomAsync(string roomId, int quizSetId)
        {
            var quizzes = await _quizRepository.GetQuizzesByQuizSetIdAsync(new Guid(quizSetId.ToString()));
            var questions = new List<GameQuestionDto>();

            foreach (var quiz in quizzes)
            {
                var answerOptions = await _answerOptionRepository.GetByQuizIdAsync(quiz.Id);
                
                var question = new GameQuestionDto
                {
                    QuestionId = quiz.Id.GetHashCode(), // Convert Guid to int
                    QuestionText = quiz.QuestionText,
                    TimeLimit = 30, // Default time limit
                    AnswerOptions = answerOptions.Select(ao => new GameAnswerOptionDto
                    {
                        Id = ao.Id.GetHashCode(), // Convert Guid to int
                        OptionText = ao.OptionText,
                        IsCorrect = ao.IsCorrect
                    }).ToList()
                };

                questions.Add(question);
            }

            _roomQuestions[roomId] = questions;
        }

        private int CalculateScore(int? userId, List<SubmitAnswerDto> answers, List<GameQuestionDto> questions)
        {
            if (!userId.HasValue) return 0;

            var userAnswers = answers.Where(a => a.UserId == userId.Value).ToList();
            var score = 0;

            foreach (var answer in userAnswers)
            {
                var question = questions.FirstOrDefault(q => q.QuestionId == answer.QuestionId);
                if (question != null)
                {
                    var correctOption = question.AnswerOptions.FirstOrDefault(ao => ao.IsCorrect);
                    if (correctOption != null && correctOption.Id == answer.AnswerOptionId)
                    {
                        // Calculate points based on time spent (faster = more points)
                        var timeBonus = Math.Max(0, 10 - (int)answer.TimeSpent);
                        score += 10 + timeBonus;
                    }
                }
            }

            return score;
        }
    }
}
