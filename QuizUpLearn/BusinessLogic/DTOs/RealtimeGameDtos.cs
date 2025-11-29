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

        // Boss Fight Mode stats
        public int TotalDamage { get; set; } = 0; // Total damage dealt to boss
        public int CorrectAnswers { get; set; } = 0; // Number of correct answers
        public int TotalAnswered { get; set; } = 0; // Total questions answered (for tracking X/Y correct format)
        
        // Per-player question tracking for Boss Fight infinite loop mode
        public int CurrentQuestionIndex { get; set; } = 0; // Current question index for this player
        public int QuestionLoopCount { get; set; } = 0; // How many times player has looped through all questions
        public List<int> ShuffledQuestionOrder { get; set; } = new(); // Shuffled order of question indices
        public HashSet<Guid> AnsweredQuestionIds { get; set; } = new(); // Questions answered in current loop
        public DateTime? PlayerQuestionStartedAt { get; set; } // When the current question was sent to this player
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
        public Dictionary<Guid, QuizGroupItemDto> QuizGroupItems { get; set; } = new(); // GroupItemId -> GroupItem
        public int CurrentQuestionIndex { get; set; } = 0;
        public DateTime? QuestionStartedAt { get; set; }
        public Dictionary<string, PlayerAnswer> CurrentAnswers { get; set; } = new(); // ConnectionId -> Answer
        public DateTime CreatedAt { get; set; }

        // ==================== BOSS FIGHT MODE ====================
        public bool IsBossFightMode { get; set; } = false;
        public int BossMaxHP { get; set; } = 10000; // Default boss HP
        public int BossCurrentHP { get; set; } = 10000;
        public int TotalDamageDealt { get; set; } = 0;
        public bool BossDefeated { get; set; } = false;
        public int? GameTimeLimitSeconds { get; set; } // Overall time limit for boss fight
        public int QuestionTimeLimitSeconds { get; set; } = 30; // Per-question time limit in seconds
        public DateTime? GameStartedAt { get; set; } // When game actually started
        public bool AutoNextQuestion { get; set; } = false; // Continuous question flow
    }

    /// <summary>
    /// Event data khi boss nhận damage
    /// </summary>
    public class BossDamagedDto
    {
        public string PlayerName { get; set; } = string.Empty;
        public int DamageDealt { get; set; }
        public int BossCurrentHP { get; set; }
        public int BossMaxHP { get; set; }
        public int TotalDamageDealt { get; set; }
        public double BossHPPercent => BossMaxHP > 0 ? (double)BossCurrentHP / BossMaxHP * 100 : 0;
    }

    /// <summary>
    /// Event data khi boss bị đánh bại
    /// </summary>
    public class BossDefeatedDto
    {
        public string GamePin { get; set; } = string.Empty;
        public int TotalDamageDealt { get; set; }
        public List<PlayerDamageRanking> DamageRankings { get; set; } = new();
        public PlayerDamageRanking? MvpPlayer { get; set; }
        public double TimeToDefeat { get; set; } // seconds
        public bool BossWins { get; set; } = false; // True when boss wins (time up / questions exhausted)
    }

    /// <summary>
    /// Player damage ranking for boss fight
    /// </summary>
    public class PlayerDamageRanking
    {
        public string PlayerName { get; set; } = string.Empty;
        public int TotalDamage { get; set; }
        public int CorrectAnswers { get; set; }
        public int TotalAnswered { get; set; } // Total questions answered by this player
        public int Rank { get; set; }
        public double DamagePercent { get; set; }
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
        
        // TOEIC-style grouped question support
        public Guid? QuizGroupItemId { get; set; } // Reference to group item (for grouped questions)
    }

    /// <summary>
    /// Quiz Group Item DTO (for TOEIC-style grouped questions with shared passages/audio/images)
    /// </summary>
    public class QuizGroupItemDto
    {
        public Guid Id { get; set; }
        public string? Name { get; set; }
        public string? AudioUrl { get; set; }
        public string? ImageUrl { get; set; }
        public string? PassageText { get; set; }
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
        public int CorrectAnswers { get; set; } // Cumulative correct answers
        public int TotalAnswered { get; set; } // Cumulative total answered
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
        public int TotalAnswered { get; set; } // Total questions answered by this player
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
        
        // Boss Fight mode properties
        public bool IsBossFightMode { get; set; } = false;
        public bool BossDefeated { get; set; } = false;
        public int BossMaxHP { get; set; }
        public int BossCurrentHP { get; set; }
        public int TotalDamageDealt { get; set; }
        
        // Force end properties
        public bool ForceEnded { get; set; } = false;
        public string? ForceEndReason { get; set; }
    }
}
