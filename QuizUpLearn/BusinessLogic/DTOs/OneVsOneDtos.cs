using System.ComponentModel.DataAnnotations;

namespace BusinessLogic.DTOs
{
    // ==================== CREATE 1VS1 ROOM ====================
    /// <summary>
    /// DTO để tạo phòng 1vs1 (người tạo cũng là player)
    /// </summary>
    public class CreateOneVsOneRoomDto
    {
        [Required]
        public Guid Player1UserId { get; set; }
        
        [Required]
        public string Player1Name { get; set; } = string.Empty;
        
        [Required]
        public Guid QuizSetId { get; set; }
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

    // ==================== ROOM STATE ====================
    /// <summary>
    /// Thông tin player trong phòng 1vs1
    /// </summary>
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
    /// Thông tin phòng 1vs1
    /// </summary>
    public class OneVsOneRoomDto
    {
        public string RoomPin { get; set; } = string.Empty;
        public Guid RoomId { get; set; }
        public Guid QuizSetId { get; set; }
        public OneVsOneRoomStatus Status { get; set; }
        public OneVsOnePlayerDto? Player1 { get; set; } // Người tạo phòng
        public OneVsOnePlayerDto? Player2 { get; set; } // Người join vào
        public List<QuestionDto> Questions { get; set; } = new();
        public int CurrentQuestionIndex { get; set; } = 0;
        public DateTime? QuestionStartedAt { get; set; }
        public OneVsOneRoundResultDto? CurrentRoundResult { get; set; } // Kết quả câu hiện tại
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
    /// Kết quả một round (câu hỏi) trong 1vs1
    /// Hiển thị ngay sau khi cả 2 đã trả lời
    /// </summary>
    public class OneVsOneRoundResultDto
    {
        public Guid QuestionId { get; set; }
        public int QuestionNumber { get; set; }
        public int TotalQuestions { get; set; }
        public Guid CorrectAnswerId { get; set; }
        public string CorrectAnswerText { get; set; } = string.Empty;
        public OneVsOnePlayerResult? Player1Result { get; set; }
        public OneVsOnePlayerResult? Player2Result { get; set; }
        public string? WinnerName { get; set; } // Người thắng round này (nếu có)
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
    /// Kết quả cuối cùng của game 1vs1
    /// </summary>
    public class OneVsOneFinalResultDto
    {
        public string RoomPin { get; set; } = string.Empty;
        public OneVsOnePlayerDto? Winner { get; set; }
        public OneVsOnePlayerDto? Player1 { get; set; }
        public OneVsOnePlayerDto? Player2 { get; set; }
        public int TotalQuestions { get; set; }
        public DateTime CompletedAt { get; set; }
    }

    // ==================== ENUMS ====================
    public enum OneVsOneRoomStatus
    {
        Waiting = 0,        // Đang chờ player 2 join
        Ready = 1,          // Đủ 2 người, chờ start
        InProgress = 2,     // Đang chơi
        ShowingResult = 3,  // Đang hiển thị kết quả round
        Completed = 4,     // Đã kết thúc
        Cancelled = 5       // Đã hủy
    }
}

