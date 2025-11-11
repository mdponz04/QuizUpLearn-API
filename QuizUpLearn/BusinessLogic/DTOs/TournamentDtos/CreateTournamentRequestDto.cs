namespace BusinessLogic.DTOs.TournamentDtos
{
	public class CreateTournamentRequestDto
	{
		public string Name { get; set; } = string.Empty;
		public string Description { get; set; } = string.Empty;
		public DateTime StartDate { get; set; }
		public DateTime EndDate { get; set; }
		public long MaxParticipants { get; set; }
		public Guid CreatedBy { get; set; }
		public IEnumerable<Guid> QuizSetIds { get; set; } = new List<Guid>();
	}
}


