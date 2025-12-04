namespace BusinessLogic.DTOs
{
    public class RequestStartMistakeQuizzesDto
    {
        /// <summary>
        /// User sẽ làm lại các câu sai (lấy từ bảng UserMistake)
        /// </summary>
        public required Guid UserId { get; set; }
    }
}

