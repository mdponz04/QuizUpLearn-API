namespace BusinessLogic.DTOs.TournamentDtos
{
	public class TournamentQuizSetItemDto
	{
		public Guid Id { get; set; }
		public Guid TournamentId { get; set; }
		public Guid QuizSetId { get; set; }
		public DateTime UnlockDate { get; set; }
		public bool IsActive { get; set; }
		public int DayNumber { get; set; }
	}
}


