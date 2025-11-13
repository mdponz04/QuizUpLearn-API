namespace BusinessLogic.DTOs
{
    public class ResponsePlacementTestDto
    {
        public Guid AttemptId { get; set; }
        public int LisPoint { get; set; }
        public int TotalCorrectLisAns { get; set; }
        public int ReaPoint { get; set; }
        public int TotalCorrectReaAns { get; set; }
        public int TotalQuestions { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}


