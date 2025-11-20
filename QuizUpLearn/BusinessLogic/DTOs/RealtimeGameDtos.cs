using Repository.Enums;
using System.ComponentModel.DataAnnotations;

namespace BusinessLogic.DTOs
{
    // ==================== HOST CREATES GAME ====================
    /// <summary>
    /// DTO để Host tạo game session mới
    /// </summary>
    public class CreateGameDto
    {
        [Required]
        public Guid HostUserId { get; set; }
        
        [Required]
        public string HostUserName { get; set; } = string.Empty;
        
        [Required]
        public Guid QuizSetId { get; set; }
    }

    /// <summary>
    /// Response trả về khi tạo game thành công
    /// </summary>
    public class CreateGameResponseDto
    {
        public string GamePin { get; set; } = string.Empty;
        public Guid GameSessionId { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    // ==================== LOBBY (WAITING ROOM) ====================
    /// <summary>
    /// Player info khi join vào lobby
    /// </summary>
    public class PlayerInfo
    {
        public string ConnectionId { get; set; } = string.Empty;
        public string PlayerName { get; set; } = string.Empty;
        public int Score { get; set; } = 0;
        public DateTime JoinedAt { get; set; }
    }

    /// <summary>
    /// Thông tin game session đầy đủ
    /// </summary>
    public class GameSessionDto
    {
        public string GamePin { get; set; } = string.Empty;
        public Guid GameSessionId { get; set; }
        public Guid HostUserId { get; set; }
        public string HostUserName { get; set; } = string.Empty;
        public string HostConnectionId { get; set; } = string.Empty;
        public Guid QuizSetId { get; set; }
        public GameStatus Status { get; set; }
        public List<PlayerInfo> Players { get; set; } = new();
        public List<QuestionDto> Questions { get; set; } = new();
        public int CurrentQuestionIndex { get; set; } = 0;
        public DateTime? QuestionStartedAt { get; set; }
        public Dictionary<string, PlayerAnswer> CurrentAnswers { get; set; } = new(); // ConnectionId -> Answer
        public DateTime CreatedAt { get; set; }
    }

    // ==================== QUESTIONS ====================
    /// <summary>
    /// Câu hỏi gửi cho client (KHÔNG chứa đáp án đúng)
    /// </summary>
    public class QuestionDto
    {
        public Guid QuestionId { get; set; }
        public string QuestionText { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public string? AudioUrl { get; set; }
        public List<AnswerOptionDto> AnswerOptions { get; set; } = new();
        public int QuestionNumber { get; set; }
        public int TotalQuestions { get; set; }
        public int? TimeLimit { get; set; } // seconds, do Host đặt cho từng câu
    }

    /// <summary>
    /// Đáp án (client nhìn thấy - không có IsCorrect)
    /// </summary>
    public class AnswerOptionDto
    {
        public Guid AnswerId { get; set; }
        public string OptionText { get; set; } = string.Empty;
    }

    // ==================== SUBMIT ANSWER ====================
    /// <summary>
    /// Player submit answer
    /// </summary>
    public class PlayerAnswer
    {
        public string ConnectionId { get; set; } = string.Empty;
        public string PlayerName { get; set; } = string.Empty;
        public Guid QuestionId { get; set; }
        public Guid AnswerId { get; set; }
        public double TimeSpent { get; set; } // seconds
        public bool IsCorrect { get; set; }
        public int PointsEarned { get; set; }
        public DateTime SubmittedAt { get; set; }
    }

    // ==================== ANSWER RESULT ====================
    /// <summary>
    /// Kết quả sau khi hết giờ - hiển thị đáp án đúng và thống kê
    /// </summary>
    public class GameAnswerResultDto
    {
        public Guid QuestionId { get; set; }
        public Guid CorrectAnswerId { get; set; }
        public string CorrectAnswerText { get; set; } = string.Empty;
        public Dictionary<Guid, int> AnswerStats { get; set; } = new(); // AnswerId -> Count
        public List<PlayerAnswerResult> PlayerResults { get; set; } = new();
    }

    public class PlayerAnswerResult
    {
        public string PlayerName { get; set; } = string.Empty;
        public bool IsCorrect { get; set; }
        public int PointsEarned { get; set; }
        public double TimeSpent { get; set; }
    }

    // ==================== LEADERBOARD ====================
    /// <summary>
    /// Bảng xếp hạng
    /// </summary>
    public class LeaderboardDto
    {
        public List<PlayerScore> Rankings { get; set; } = new();
        public int CurrentQuestion { get; set; }
        public int TotalQuestions { get; set; }
    }

    public class PlayerScore
    {
        public string PlayerName { get; set; } = string.Empty;
        public int TotalScore { get; set; }
        public int CorrectAnswers { get; set; }
        public int Rank { get; set; }
    }

    // ==================== GAME END ====================
    /// <summary>
    /// Kết quả cuối cùng khi game kết thúc
    /// </summary>
    public class FinalResultDto
    {
        public string GamePin { get; set; } = string.Empty;
        public List<PlayerScore> FinalRankings { get; set; } = new();
        public PlayerScore? Winner { get; set; }
        public DateTime CompletedAt { get; set; }
        public int TotalPlayers { get; set; }
        public int TotalQuestions { get; set; }
    }
}
