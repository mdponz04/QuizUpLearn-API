using System.ComponentModel.DataAnnotations;

namespace BusinessLogic.DTOs
{
    // DTO cho tạo phòng game
    public class CreateGameRoomDto
    {
        [Required]
        public int HostUserId { get; set; }
        public string? HostUserName { get; set; }
        [Required]
        public int QuizSetId { get; set; }
        public int TimeLimit { get; set; } = 30;
    }

    // DTO cho join phòng
    public class JoinGameRoomDto
    {
        [Required]
        public string RoomId { get; set; } = string.Empty;
        [Required]
        public int UserId { get; set; }
        public string? UserName { get; set; }
    }

    // DTO cho thông tin phòng
    public class GameRoomInfoDto
    {
        public string RoomId { get; set; } = string.Empty;
        public int HostUserId { get; set; }
        public string? HostUserName { get; set; }
        public int? GuestUserId { get; set; }
        public string? GuestUserName { get; set; }
        public int QuizSetId { get; set; }
        public int TimeLimit { get; set; }
        public GameRoomStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    // DTO cho câu hỏi
    public class GameQuestionDto
    {
        public int QuestionId { get; set; }
        public string QuestionText { get; set; } = string.Empty;
        public List<GameAnswerOptionDto> AnswerOptions { get; set; } = new();
        public int TimeLimit { get; set; }
        public int QuestionNumber { get; set; }
        public int TotalQuestions { get; set; }
    }

    // DTO cho đáp án
    public class GameAnswerOptionDto
    {
        public int Id { get; set; }
        public string OptionText { get; set; } = string.Empty;
        public bool IsCorrect { get; set; }
    }

    // DTO cho trả lời
    public class SubmitAnswerDto
    {
        [Required]
        public string RoomId { get; set; } = string.Empty;
        [Required]
        public int UserId { get; set; }
        [Required]
        public int QuestionId { get; set; }
        [Required]
        public int AnswerOptionId { get; set; }
        public double TimeSpent { get; set; }
    }

    // DTO cho kết quả game
    public class GameResultDto
    {
        public string RoomId { get; set; } = string.Empty;
        public int HostUserId { get; set; }
        public string? HostUserName { get; set; }
        public int HostScore { get; set; }
        public int? GuestUserId { get; set; }
        public string? GuestUserName { get; set; }
        public int GuestScore { get; set; }
        public int WinnerUserId { get; set; }
        public string? WinnerUserName { get; set; }
        public DateTime CompletedAt { get; set; }
    }

    // DTO cho start game
    public class StartGameDto
    {
        [Required]
        public string RoomId { get; set; } = string.Empty;
        [Required]
        public int UserId { get; set; }
    }

    // DTO cho leave game
    public class LeaveGameDto
    {
        [Required]
        public string RoomId { get; set; } = string.Empty;
        [Required]
        public int UserId { get; set; }
    }

    // Enum cho trạng thái phòng
    public enum GameRoomStatus
    {
        Waiting = 0,
        InProgress = 1,
        Completed = 2,
        Cancelled = 3
    }
}
