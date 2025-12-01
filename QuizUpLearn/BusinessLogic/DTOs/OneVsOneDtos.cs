using Repository.Enums;
using System.ComponentModel.DataAnnotations;

namespace BusinessLogic.DTOs
{ 
    // ==================== CREATE ROOM ====================
    /// <summary>
    /// DTO để tạo phòng (1vs1 hoặc Multiplayer)
    /// Player1UserId sẽ được tự động lấy từ JWT token trong Controller
    /// </summary>
    public class CreateOneVsOneRoomDto
    {        
        [Required]
        public string Player1Name { get; set; } = string.Empty;
        
        [Required]
        public Guid QuizSetId { get; set; }
        
        /// <summary>
        /// UserId của Player1 (sẽ được set tự động từ JWT token trong Controller)
        /// </summary>
        public Guid Player1UserId { get; set; }
        
        /// <summary>
        /// Chế độ chơi:
        /// - 0 (OneVsOne): Giới hạn 2 players (backend tự set MaxPlayers = 2)
        /// - 1 (Multiplayer): Không giới hạn players (backend tự set MaxPlayers = null)
        /// </summary>
        public GameModeEnum Mode { get; set; } = GameModeEnum.OneVsOne;
    }

    /// <summary>
    /// Response khi tạo phòng thành công
    /// </summary>
    public class CreateOneVsOneRoomResponseDto
    {
        public string RoomPin { get; set; } = string.Empty;
        public Guid RoomId { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class OneVsOnePlayerDto
    {
        public string ConnectionId { get; set; } = string.Empty;
        public Guid UserId { get; set; }
        public string PlayerName { get; set; } = string.Empty;
        public int Score { get; set; } = 0;
        public int CorrectAnswers { get; set; } = 0;
        public DateTime JoinedAt { get; set; }
        public bool IsReady { get; set; } = false; // Sẵn sàng chơi
    }

    /// <summary>
    /// Thông tin phòng (1vs1 hoặc Multiplayer)
    /// </summary>
    public class OneVsOneRoomDto
    {
        public string RoomPin { get; set; } = string.Empty;
        public Guid RoomId { get; set; }
        public Guid QuizSetId { get; set; }
        public OneVsOneRoomStatus Status { get; set; }
        
        /// <summary>
        /// Chế độ chơi
        /// </summary>
        public GameModeEnum Mode { get; set; } = GameModeEnum.OneVsOne;
        
        /// <summary>
        /// Số lượng players tối đa (null = unlimited)
        /// </summary>
        public int? MaxPlayers { get; set; } = 2;
        
        /// <summary>
        /// Danh sách tất cả players (Players[0] = Host/Player1)
        /// </summary>
        public List<OneVsOnePlayerDto> Players { get; set; } = new();
        
        /// <summary>
        /// Player1 (Host) - Backward compatibility, populated từ Players[0]
        /// </summary>
        public OneVsOnePlayerDto? Player1 { get; set; }
        
        /// <summary>
        /// Player2 - Backward compatibility cho 1vs1, populated từ Players[1]
        /// </summary>
        public OneVsOnePlayerDto? Player2 { get; set; }
        
        public List<QuestionDto> Questions { get; set; } = new();
        
        /// <summary>
        /// Quiz Group Items for TOEIC-style grouped questions (Parts 3,4,6,7)
        /// Key = QuizGroupItemId, Value = QuizGroupItemDto
        /// </summary>
        public Dictionary<Guid, QuizGroupItemDto> QuizGroupItems { get; set; } = new();
        
        public int CurrentQuestionIndex { get; set; } = 0;
        public DateTime? QuestionStartedAt { get; set; }
        
        /// <summary>
        /// Câu trả lời hiện tại (ConnectionId -> Answer)
        /// </summary>
        public Dictionary<string, OneVsOneAnswerDto> CurrentAnswers { get; set; } = new();
        
        public OneVsOneRoundResultDto? CurrentRoundResult { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    // ==================== QUESTIONS ====================
    /// <summary>
    /// Câu hỏi trong phòng 1vs1 (tái sử dụng QuestionDto từ RealtimeGameDtos)
    /// </summary>

    // ==================== SUBMIT ANSWER ====================
    /// <summary>
    /// Player submit answer trong 1vs1
    /// </summary>
    public class OneVsOneAnswerDto
    {
        public string ConnectionId { get; set; } = string.Empty;
        public Guid UserId { get; set; }
        public string PlayerName { get; set; } = string.Empty;
        public Guid QuestionId { get; set; }
        public Guid AnswerId { get; set; }
        public bool IsCorrect { get; set; }
        public int PointsEarned { get; set; }
        public double TimeSpent { get; set; } // seconds
        public DateTime SubmittedAt { get; set; }
    }

    // ==================== ROUND RESULT ====================
    /// <summary>
    /// Kết quả một round (câu hỏi) - hỗ trợ cả 1vs1 và Multiplayer
    /// Hiển thị ngay sau khi tất cả players đã trả lời hoặc hết giờ
    /// </summary>
    public class OneVsOneRoundResultDto
    {
        public Guid QuestionId { get; set; }
        public int QuestionNumber { get; set; }
        public int TotalQuestions { get; set; }
        public Guid CorrectAnswerId { get; set; }
        public string CorrectAnswerText { get; set; } = string.Empty;
        
        /// <summary>
        /// Danh sách kết quả của tất cả players (sorted by points descending)
        /// </summary>
        public List<OneVsOnePlayerResult> PlayerResults { get; set; } = new();
        
        /// <summary>
        /// Player1 result - Backward compatibility, populated từ PlayerResults[0]
        /// </summary>
        public OneVsOnePlayerResult? Player1Result { get; set; }
        
        /// <summary>
        /// Player2 result - Backward compatibility, populated từ PlayerResults[1]
        /// </summary>
        public OneVsOnePlayerResult? Player2Result { get; set; }
        
        public string? WinnerName { get; set; } // Người thắng round (highest points)
    }

    /// <summary>
    /// Kết quả của một player trong round
    /// </summary>
    public class OneVsOnePlayerResult
    {
        public string PlayerName { get; set; } = string.Empty;
        public Guid AnswerId { get; set; }
        public bool IsCorrect { get; set; }
        public int PointsEarned { get; set; }
        public double TimeSpent { get; set; }
    }

    // ==================== FINAL RESULT ====================
    /// <summary>
    /// Kết quả cuối cùng của game (1vs1 hoặc Multiplayer)
    /// </summary>
    public class OneVsOneFinalResultDto
    {
        public string RoomPin { get; set; } = string.Empty;
        public GameModeEnum Mode { get; set; }
        
        /// <summary>
        /// Rankings của tất cả players (sorted by score descending)
        /// </summary>
        public List<OneVsOnePlayerDto> Rankings { get; set; } = new();
        
        /// <summary>
        /// Player1 - Backward compatibility, populated từ Rankings[0]
        /// </summary>
        public OneVsOnePlayerDto? Player1 { get; set; }
        
        /// <summary>
        /// Player2 - Backward compatibility, populated từ Rankings[1]
        /// </summary>
        public OneVsOnePlayerDto? Player2 { get; set; }
        
        public OneVsOnePlayerDto? Winner { get; set; }
        public int TotalQuestions { get; set; }
        public DateTime CompletedAt { get; set; }
    }

    // ==================== ENUMS ====================

}

