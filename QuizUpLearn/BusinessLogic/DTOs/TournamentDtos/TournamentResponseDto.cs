namespace BusinessLogic.DTOs.TournamentDtos
{
	public class TournamentResponseDto
	{
		public Guid Id { get; set; }
		public string Name { get; set; } = string.Empty;
		public string Description { get; set; } = string.Empty;
		public DateTime StartDate { get; set; }
		public DateTime EndDate { get; set; }
		public long MaxParticipants { get; set; }
		public string Status { get; set; } = string.Empty;
		public int TotalQuizSets { get; set; }
	}
}


