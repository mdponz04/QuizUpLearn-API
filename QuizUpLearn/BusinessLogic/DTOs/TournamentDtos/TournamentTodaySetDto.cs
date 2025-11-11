namespace BusinessLogic.DTOs.TournamentDtos
{
	public class TournamentTodaySetDto
	{
		public Guid TournamentId { get; set; }
		public DateTime Date { get; set; }
		public Guid QuizSetId { get; set; }
		public int DayNumber { get; set; }
	}
}


