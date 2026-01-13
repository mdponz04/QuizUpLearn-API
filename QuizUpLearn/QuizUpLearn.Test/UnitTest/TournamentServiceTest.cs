using BusinessLogic.DTOs.TournamentDtos;
using BusinessLogic.Interfaces;
using BusinessLogic.MappingProfile;
using BusinessLogic.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Repository.Entities;
using Repository.Enums;
using Repository.Interfaces;

namespace QuizUpLearn.Test.UnitTest
{
	public class TournamentServiceTest : BaseControllerTest
	{
		private readonly Mock<ITournamentRepo> _mockTournamentRepo;
		private readonly Mock<ITournamentParticipantRepo> _mockParticipantRepo;
		private readonly Mock<ITournamentQuizSetRepo> _mockTournamentQuizSetRepo;
		private readonly Mock<IQuizSetRepo> _mockQuizSetRepo;
		private readonly Mock<IQuizAttemptRepo> _mockQuizAttemptRepo;
		private readonly Mock<IUserRepo> _mockUserRepo;
		private readonly TournamentService _tournamentService;

		public TournamentServiceTest()
		{
			_mockTournamentRepo = new Mock<ITournamentRepo>();
			_mockParticipantRepo = new Mock<ITournamentParticipantRepo>();
			_mockTournamentQuizSetRepo = new Mock<ITournamentQuizSetRepo>();
			_mockQuizSetRepo = new Mock<IQuizSetRepo>();
			_mockQuizAttemptRepo = new Mock<IQuizAttemptRepo>();
			_mockUserRepo = new Mock<IUserRepo>();

			// Setup default logger
			var logger = new NullLogger<TournamentService>();

			_tournamentService = new TournamentService(
				_mockTournamentRepo.Object,
				_mockParticipantRepo.Object,
				_mockTournamentQuizSetRepo.Object,
				_mockQuizSetRepo.Object,
				_mockQuizAttemptRepo.Object,
				_mockUserRepo.Object,
				logger);
		}

		[Fact]
		public async Task CreateAsync_WithValidData_ShouldReturnTournamentResponse()
		{
			// Arrange
			var createDto = new CreateTournamentRequestDto
			{
				Name = "Test Tournament",
				Description = "Test Description",
				StartDate = DateTime.UtcNow.AddDays(1),
				EndDate = DateTime.UtcNow.AddDays(30),
				MaxParticipants = 100,
				CreatedBy = Guid.NewGuid(),
				QuizSetIds = new List<Guid>()
			};

			var createdTournament = new Tournament
			{
				Id = Guid.NewGuid(),
				Name = createDto.Name,
				Description = createDto.Description,
				StartDate = createDto.StartDate,
				EndDate = createDto.EndDate,
				MaxParticipants = createDto.MaxParticipants,
				Status = "Created",
				CreatedBy = createDto.CreatedBy,
				CreatedAt = DateTime.UtcNow
			};

			_mockTournamentRepo.Setup(r => r.ExistsStartedInMonthAsync(It.IsAny<int>(), It.IsAny<int>()))
				.ReturnsAsync(false);
			_mockTournamentRepo.Setup(r => r.CreateAsync(It.IsAny<Tournament>()))
				.ReturnsAsync(createdTournament);
			_mockTournamentQuizSetRepo.Setup(r => r.GetByTournamentAsync(It.IsAny<Guid>()))
				.ReturnsAsync(new List<TournamentQuizSet>());

			// Act
			var result = await _tournamentService.CreateAsync(createDto);

			// Assert
			result.Should().NotBeNull();
			result.Id.Should().Be(createdTournament.Id);
			result.Name.Should().Be(createDto.Name);
			result.Description.Should().Be(createDto.Description);
			result.Status.Should().Be("Created");
			result.MaxParticipants.Should().Be(createDto.MaxParticipants);

			_mockTournamentRepo.Verify(r => r.CreateAsync(It.Is<Tournament>(t =>
				t.Name == createDto.Name &&
				t.Status == "Created")), Times.Once);
		}

		[Fact]
		public async Task CreateAsync_WithStartDateInPast_ShouldThrowException()
		{
			// Arrange
			var createDto = new CreateTournamentRequestDto
			{
				Name = "Test Tournament",
				StartDate = DateTime.UtcNow.AddDays(-1), // Past date
				EndDate = DateTime.UtcNow.AddDays(30),
				MaxParticipants = 100,
				CreatedBy = Guid.NewGuid(),
				QuizSetIds = new List<Guid>()
			};

			// Act & Assert
			await Assert.ThrowsAsync<ArgumentException>(async () =>
				await _tournamentService.CreateAsync(createDto));

			_mockTournamentRepo.Verify(r => r.CreateAsync(It.IsAny<Tournament>()), Times.Never);
		}

		[Fact]
		public async Task CreateAsync_WithStartDateAfterEndDate_ShouldThrowException()
		{
			// Arrange
			var createDto = new CreateTournamentRequestDto
			{
				Name = "Test Tournament",
				StartDate = DateTime.UtcNow.AddDays(30),
				EndDate = DateTime.UtcNow.AddDays(1), // EndDate before StartDate
				MaxParticipants = 100,
				CreatedBy = Guid.NewGuid(),
				QuizSetIds = new List<Guid>()
			};

			// Act & Assert
			await Assert.ThrowsAsync<ArgumentException>(async () =>
				await _tournamentService.CreateAsync(createDto));

			_mockTournamentRepo.Verify(r => r.CreateAsync(It.IsAny<Tournament>()), Times.Never);
		}

		[Fact]
		public async Task GetByIdAsync_WithValidId_ShouldReturnTournamentResponse()
		{
			// Arrange
			var tournamentId = Guid.NewGuid();
			var tournament = new Tournament
			{
				Id = tournamentId,
				Name = "Test Tournament",
				Description = "Test Description",
				Status = "Created",
				StartDate = DateTime.UtcNow.AddDays(1),
				EndDate = DateTime.UtcNow.AddDays(30),
				MaxParticipants = 100,
				CreatedAt = DateTime.UtcNow
			};

			_mockTournamentRepo.Setup(r => r.GetByIdAsync(tournamentId))
				.ReturnsAsync(tournament);
			_mockTournamentQuizSetRepo.Setup(r => r.GetByTournamentAsync(tournamentId))
				.ReturnsAsync(new List<TournamentQuizSet>());

			// Act
			var result = await _tournamentService.GetByIdAsync(tournamentId);

			// Assert
			result.Should().NotBeNull();
			result!.Id.Should().Be(tournamentId);
			result.Name.Should().Be(tournament.Name);
			result.Status.Should().Be(tournament.Status);

			_mockTournamentRepo.Verify(r => r.GetByIdAsync(tournamentId), Times.Once);
		}

		[Fact]
		public async Task GetByIdAsync_WithNonExistentId_ShouldReturnNull()
		{
			// Arrange
			var tournamentId = Guid.NewGuid();
			_mockTournamentRepo.Setup(r => r.GetByIdAsync(tournamentId))
				.ReturnsAsync((Tournament?)null);

			// Act
			var result = await _tournamentService.GetByIdAsync(tournamentId);

			// Assert
			result.Should().BeNull();
			_mockTournamentRepo.Verify(r => r.GetByIdAsync(tournamentId), Times.Once);
		}

		[Fact]
		public async Task GetAllAsync_ShouldReturnAllTournaments()
		{
			// Arrange
			var tournaments = new List<Tournament>
			{
				new Tournament
				{
					Id = Guid.NewGuid(),
					Name = "Tournament 1",
					Status = "Created",
					CreatedAt = DateTime.UtcNow
				},
				new Tournament
				{
					Id = Guid.NewGuid(),
					Name = "Tournament 2",
					Status = "Started",
					CreatedAt = DateTime.UtcNow
				}
			};

			_mockTournamentRepo.Setup(r => r.GetAllAsync(It.IsAny<bool>()))
				.ReturnsAsync(tournaments);
			_mockTournamentQuizSetRepo.Setup(r => r.GetByTournamentAsync(It.IsAny<Guid>()))
				.ReturnsAsync(new List<TournamentQuizSet>());

			// Act
			var result = await _tournamentService.GetAllAsync();

			// Assert
			result.Should().NotBeNull();
			result.Should().HaveCount(2);
			_mockTournamentRepo.Verify(r => r.GetAllAsync(It.IsAny<bool>()), Times.Once);
		}

		[Fact]
		public async Task GetByMonthAsync_ShouldReturnTournamentsForMonth()
		{
			// Arrange
			var year = DateTime.UtcNow.Year;
			var month = DateTime.UtcNow.Month;
			var tournaments = new List<Tournament>
			{
				new Tournament
				{
					Id = Guid.NewGuid(),
					Name = "Monthly Tournament",
					Status = "Created",
					CreatedAt = DateTime.UtcNow
				}
			};

			_mockTournamentRepo.Setup(r => r.GetByMonthAsync(year, month, It.IsAny<bool>()))
				.ReturnsAsync(tournaments);
			_mockTournamentQuizSetRepo.Setup(r => r.GetByTournamentAsync(It.IsAny<Guid>()))
				.ReturnsAsync(new List<TournamentQuizSet>());

			// Act
			var result = await _tournamentService.GetByMonthAsync(year, month);

			// Assert
			result.Should().NotBeNull();
			result.Should().HaveCount(1);
			_mockTournamentRepo.Verify(r => r.GetByMonthAsync(year, month, It.IsAny<bool>()), Times.Once);
		}

		[Fact]
		public async Task StartAsync_WithCreatedStatus_ShouldReturnStartedTournament()
		{
			// Arrange
			var tournamentId = Guid.NewGuid();
			var tournament = new Tournament
			{
				Id = tournamentId,
				Name = "Test Tournament",
				Status = "Created",
				StartDate = DateTime.UtcNow.AddDays(1),
				EndDate = DateTime.UtcNow.AddDays(30),
				CreatedAt = DateTime.UtcNow
			};

			_mockTournamentRepo.Setup(r => r.GetByIdAsync(tournamentId))
				.ReturnsAsync(tournament);
			_mockTournamentRepo.Setup(r => r.ExistsStartedInMonthAsync(It.IsAny<int>(), It.IsAny<int>()))
				.ReturnsAsync(false);
			_mockTournamentRepo.Setup(r => r.UpdateAsync(It.IsAny<Tournament>()))
				.ReturnsAsync((Tournament t) => t);
			_mockTournamentQuizSetRepo.Setup(r => r.GetByTournamentAsync(tournamentId))
				.ReturnsAsync(new List<TournamentQuizSet>());

			// Act
			var result = await _tournamentService.StartAsync(tournamentId);

			// Assert
			result.Should().NotBeNull();
			result.Status.Should().Be("Started");
			_mockTournamentRepo.Verify(r => r.UpdateAsync(It.Is<Tournament>(t =>
				t.Status == "Started")), Times.Once);
		}

		[Fact]
		public async Task StartAsync_WithNonCreatedStatus_ShouldThrowException()
		{
			// Arrange
			var tournamentId = Guid.NewGuid();
			var tournament = new Tournament
			{
				Id = tournamentId,
				Status = "Started" // Already started
			};

			_mockTournamentRepo.Setup(r => r.GetByIdAsync(tournamentId))
				.ReturnsAsync(tournament);

			// Act & Assert
			await Assert.ThrowsAsync<ArgumentException>(async () =>
				await _tournamentService.StartAsync(tournamentId));

			_mockTournamentRepo.Verify(r => r.UpdateAsync(It.IsAny<Tournament>()), Times.Never);
		}

		[Fact]
		public async Task StartAsync_WithNonExistentTournament_ShouldThrowException()
		{
			// Arrange
			var tournamentId = Guid.NewGuid();
			_mockTournamentRepo.Setup(r => r.GetByIdAsync(tournamentId))
				.ReturnsAsync((Tournament?)null);

			// Act & Assert
			await Assert.ThrowsAsync<ArgumentException>(async () =>
				await _tournamentService.StartAsync(tournamentId));
		}

		[Fact]
		public async Task EndAsync_WithStartedStatus_ShouldReturnEndedTournament()
		{
			// Arrange
			var tournamentId = Guid.NewGuid();
			var tournament = new Tournament
			{
				Id = tournamentId,
				Name = "Test Tournament",
				Status = "Started",
				CreatedAt = DateTime.UtcNow
			};

			_mockTournamentRepo.Setup(r => r.GetByIdAsync(tournamentId))
				.ReturnsAsync(tournament);
			_mockTournamentRepo.Setup(r => r.UpdateAsync(It.IsAny<Tournament>()))
				.ReturnsAsync((Tournament t) => t);
			_mockTournamentQuizSetRepo.Setup(r => r.GetByTournamentAsync(tournamentId))
				.ReturnsAsync(new List<TournamentQuizSet>());

			// Act
			var result = await _tournamentService.EndAsync(tournamentId);

			// Assert
			result.Should().NotBeNull();
			result.Status.Should().Be("Ended");
			_mockTournamentRepo.Verify(r => r.UpdateAsync(It.Is<Tournament>(t =>
				t.Status == "Ended")), Times.Once);
		}

		[Fact]
		public async Task EndAsync_WithNonStartedStatus_ShouldThrowException()
		{
			// Arrange
			var tournamentId = Guid.NewGuid();
			var tournament = new Tournament
			{
				Id = tournamentId,
				Status = "Created" // Not started
			};

			_mockTournamentRepo.Setup(r => r.GetByIdAsync(tournamentId))
				.ReturnsAsync(tournament);

			// Act & Assert
			await Assert.ThrowsAsync<ArgumentException>(async () =>
				await _tournamentService.EndAsync(tournamentId));

			_mockTournamentRepo.Verify(r => r.UpdateAsync(It.IsAny<Tournament>()), Times.Never);
		}

		[Fact]
		public async Task JoinAsync_WithValidData_ShouldReturnTrue()
		{
			// Arrange
			var tournamentId = Guid.NewGuid();
			var userId = Guid.NewGuid();
			var tournament = new Tournament
			{
				Id = tournamentId,
				Name = "Test Tournament",
				Status = "Started",
				MaxParticipants = 100,
				CreatedAt = DateTime.UtcNow
			};

			_mockTournamentRepo.Setup(r => r.GetByIdAsync(tournamentId))
				.ReturnsAsync(tournament);
			_mockParticipantRepo.Setup(r => r.ExistsAsync(tournamentId, userId))
				.ReturnsAsync(false);
			_mockParticipantRepo.Setup(r => r.GetByTournamentAsync(tournamentId))
				.ReturnsAsync(new List<TournamentParticipant>());
			_mockParticipantRepo.Setup(r => r.AddAsync(It.IsAny<TournamentParticipant>()))
				.ReturnsAsync((TournamentParticipant p) => p);

			// Act
			var result = await _tournamentService.JoinAsync(tournamentId, userId);

			// Assert
			result.Should().BeTrue();
			_mockParticipantRepo.Verify(r => r.AddAsync(It.Is<TournamentParticipant>(p =>
				p.TournamentId == tournamentId && p.ParticipantId == userId)), Times.Once);
		}

		[Fact]
		public async Task JoinAsync_WhenAlreadyJoined_ShouldReturnTrue()
		{
			// Arrange
			var tournamentId = Guid.NewGuid();
			var userId = Guid.NewGuid();
			var tournament = new Tournament
			{
				Id = tournamentId,
				Status = "Started"
			};

			_mockTournamentRepo.Setup(r => r.GetByIdAsync(tournamentId))
				.ReturnsAsync(tournament);
			_mockParticipantRepo.Setup(r => r.ExistsAsync(tournamentId, userId))
				.ReturnsAsync(true); // Already joined

			// Act
			var result = await _tournamentService.JoinAsync(tournamentId, userId);

			// Assert
			result.Should().BeTrue();
			_mockParticipantRepo.Verify(r => r.AddAsync(It.IsAny<TournamentParticipant>()), Times.Never);
		}

		[Fact]
		public async Task JoinAsync_WhenTournamentNotStarted_ShouldThrowException()
		{
			// Arrange
			var tournamentId = Guid.NewGuid();
			var userId = Guid.NewGuid();
			var tournament = new Tournament
			{
				Id = tournamentId,
				Status = "Created" // Not started
			};

			_mockTournamentRepo.Setup(r => r.GetByIdAsync(tournamentId))
				.ReturnsAsync(tournament);

			// Act & Assert
			await Assert.ThrowsAsync<ArgumentException>(async () =>
				await _tournamentService.JoinAsync(tournamentId, userId));

			_mockParticipantRepo.Verify(r => r.AddAsync(It.IsAny<TournamentParticipant>()), Times.Never);
		}

		[Fact]
		public async Task JoinAsync_WhenTournamentFull_ShouldThrowException()
		{
			// Arrange
			var tournamentId = Guid.NewGuid();
			var userId = Guid.NewGuid();
			var tournament = new Tournament
			{
				Id = tournamentId,
				Status = "Started",
				MaxParticipants = 2
			};

			var existingParticipants = new List<TournamentParticipant>
			{
				new TournamentParticipant { TournamentId = tournamentId, ParticipantId = Guid.NewGuid() },
				new TournamentParticipant { TournamentId = tournamentId, ParticipantId = Guid.NewGuid() }
			};

			_mockTournamentRepo.Setup(r => r.GetByIdAsync(tournamentId))
				.ReturnsAsync(tournament);
			_mockParticipantRepo.Setup(r => r.ExistsAsync(tournamentId, userId))
				.ReturnsAsync(false);
			_mockParticipantRepo.Setup(r => r.GetByTournamentAsync(tournamentId))
				.ReturnsAsync(existingParticipants);

			// Act & Assert
			await Assert.ThrowsAsync<ArgumentException>(async () =>
				await _tournamentService.JoinAsync(tournamentId, userId));

			_mockParticipantRepo.Verify(r => r.AddAsync(It.IsAny<TournamentParticipant>()), Times.Never);
		}

		[Fact]
		public async Task IsUserJoinedAsync_WhenJoined_ShouldReturnTrue()
		{
			// Arrange
			var tournamentId = Guid.NewGuid();
			var userId = Guid.NewGuid();

			_mockParticipantRepo.Setup(r => r.ExistsAsync(tournamentId, userId))
				.ReturnsAsync(true);

			// Act
			var result = await _tournamentService.IsUserJoinedAsync(tournamentId, userId);

			// Assert
			result.Should().BeTrue();
			_mockParticipantRepo.Verify(r => r.ExistsAsync(tournamentId, userId), Times.Once);
		}

		[Fact]
		public async Task IsUserJoinedAsync_WhenNotJoined_ShouldReturnFalse()
		{
			// Arrange
			var tournamentId = Guid.NewGuid();
			var userId = Guid.NewGuid();

			_mockParticipantRepo.Setup(r => r.ExistsAsync(tournamentId, userId))
				.ReturnsAsync(false);

			// Act
			var result = await _tournamentService.IsUserJoinedAsync(tournamentId, userId);

			// Assert
			result.Should().BeFalse();
			_mockParticipantRepo.Verify(r => r.ExistsAsync(tournamentId, userId), Times.Once);
		}

		[Fact]
		public async Task DeleteAsync_WithCreatedStatus_ShouldReturnTrue()
		{
			// Arrange
			var tournamentId = Guid.NewGuid();
			var tournament = new Tournament
			{
				Id = tournamentId,
				Name = "Test Tournament",
				Status = "Created",
				CreatedAt = DateTime.UtcNow
			};

			_mockTournamentRepo.Setup(r => r.GetByIdAsync(tournamentId))
				.ReturnsAsync(tournament);
			_mockTournamentRepo.Setup(r => r.DeleteAsync(tournamentId))
				.ReturnsAsync(true);

			// Act
			var result = await _tournamentService.DeleteAsync(tournamentId);

			// Assert
			result.Should().BeTrue();
			_mockTournamentRepo.Verify(r => r.DeleteAsync(tournamentId), Times.Once);
		}

		[Fact]
		public async Task DeleteAsync_WithNonCreatedStatus_ShouldThrowException()
		{
			// Arrange
			var tournamentId = Guid.NewGuid();
			var tournament = new Tournament
			{
				Id = tournamentId,
				Status = "Started" // Cannot delete started tournament
			};

			_mockTournamentRepo.Setup(r => r.GetByIdAsync(tournamentId))
				.ReturnsAsync(tournament);

			// Act & Assert
			await Assert.ThrowsAsync<ArgumentException>(async () =>
				await _tournamentService.DeleteAsync(tournamentId));

			_mockTournamentRepo.Verify(r => r.DeleteAsync(It.IsAny<Guid>()), Times.Never);
		}

		[Fact]
		public async Task DeleteAsync_WithNonExistentTournament_ShouldThrowException()
		{
			// Arrange
			var tournamentId = Guid.NewGuid();
			_mockTournamentRepo.Setup(r => r.GetByIdAsync(tournamentId))
				.ReturnsAsync((Tournament?)null);

			// Act & Assert
			await Assert.ThrowsAsync<ArgumentException>(async () =>
				await _tournamentService.DeleteAsync(tournamentId));
		}

		[Fact]
		public async Task GetQuizSetsAsync_WithCreatedStatus_ShouldReturnQuizSets()
		{
			// Arrange
			var tournamentId = Guid.NewGuid();
			var tournament = new Tournament
			{
				Id = tournamentId,
				Status = "Created",
				CreatedAt = DateTime.UtcNow
			};

			var quizSets = new List<TournamentQuizSet>
			{
				new TournamentQuizSet
				{
					Id = Guid.NewGuid(),
					TournamentId = tournamentId,
					QuizSetId = Guid.NewGuid(),
					DateNumber = 1,
					UnlockDate = DateTime.UtcNow,
					IsActive = true
				}
			};

			_mockTournamentRepo.Setup(r => r.GetByIdAsync(tournamentId))
				.ReturnsAsync(tournament);
			_mockTournamentQuizSetRepo.Setup(r => r.GetByTournamentAsync(tournamentId))
				.ReturnsAsync(quizSets);

			// Act
			var result = await _tournamentService.GetQuizSetsAsync(tournamentId);

			// Assert
			result.Should().NotBeNull();
			result.Should().HaveCount(1);
			_mockTournamentQuizSetRepo.Verify(r => r.GetByTournamentAsync(tournamentId), Times.Once);
		}

		[Fact]
		public async Task GetQuizSetsAsync_WithNonCreatedStatus_ShouldThrowException()
		{
			// Arrange
			var tournamentId = Guid.NewGuid();
			var tournament = new Tournament
			{
				Id = tournamentId,
				Status = "Started" // Not Created
			};

			_mockTournamentRepo.Setup(r => r.GetByIdAsync(tournamentId))
				.ReturnsAsync(tournament);

			// Act & Assert
			await Assert.ThrowsAsync<ArgumentException>(async () =>
				await _tournamentService.GetQuizSetsAsync(tournamentId));
		}

		[Fact]
		public async Task GetTodaySetAsync_WithValidData_ShouldReturnTodaySet()
		{
			// Arrange
			var tournamentId = Guid.NewGuid();
			var quizSetId = Guid.NewGuid();
			var today = DateTime.UtcNow.Date;

			var quizSet = new QuizSet
			{
				Id = quizSetId,
				Title = "Today Quiz Set",
				QuizSetType = QuizSetTypeEnum.Tournament,
				CreatedAt = DateTime.UtcNow
			};

			var tournamentQuizSets = new List<TournamentQuizSet>
			{
				new TournamentQuizSet
				{
					Id = Guid.NewGuid(),
					TournamentId = tournamentId,
					QuizSetId = quizSetId,
					UnlockDate = today,
					IsActive = true,
					DateNumber = 1,
					QuizSet = quizSet
				}
			};

			_mockTournamentQuizSetRepo.Setup(r => r.GetForDateAsync(tournamentId, today))
				.ReturnsAsync(tournamentQuizSets);

			// Act
			var result = await _tournamentService.GetTodaySetAsync(tournamentId);

			// Assert
			result.Should().NotBeNull();
			result!.TournamentId.Should().Be(tournamentId);
			result.QuizSetId.Should().Be(quizSetId);
			_mockTournamentQuizSetRepo.Verify(r => r.GetForDateAsync(tournamentId, today), Times.Once);
		}

		[Fact]
		public async Task GetTodaySetAsync_WithNoQuizSets_ShouldReturnNull()
		{
			// Arrange
			var tournamentId = Guid.NewGuid();
			var today = DateTime.UtcNow.Date;

			_mockTournamentQuizSetRepo.Setup(r => r.GetForDateAsync(tournamentId, today))
				.ReturnsAsync(new List<TournamentQuizSet>());

			// Act
			var result = await _tournamentService.GetTodaySetAsync(tournamentId);

			// Assert
			result.Should().BeNull();
		}

		[Fact]
		public async Task GetLeaderboardAsync_WithNoParticipants_ShouldReturnEmptyList()
		{
			// Arrange
			var tournamentId = Guid.NewGuid();
			var tournament = new Tournament
			{
				Id = tournamentId,
				Name = "Test Tournament",
				StartDate = DateTime.UtcNow.AddDays(-10),
				EndDate = DateTime.UtcNow.AddDays(10),
				CreatedAt = DateTime.UtcNow
			};

			_mockTournamentRepo.Setup(r => r.GetByIdAsync(tournamentId))
				.ReturnsAsync(tournament);
			_mockParticipantRepo.Setup(r => r.GetByTournamentAsync(tournamentId))
				.ReturnsAsync(new List<TournamentParticipant>());

			// Act
			var result = await _tournamentService.GetLeaderboardAsync(tournamentId);

			// Assert
			result.Should().NotBeNull();
			result.Should().BeEmpty();
		}

		[Fact]
		public async Task GetLeaderboardAsync_WithNonExistentTournament_ShouldThrowException()
		{
			// Arrange
			var tournamentId = Guid.NewGuid();
			_mockTournamentRepo.Setup(r => r.GetByIdAsync(tournamentId))
				.ReturnsAsync((Tournament?)null);

			// Act & Assert
			await Assert.ThrowsAsync<ArgumentException>(async () =>
				await _tournamentService.GetLeaderboardAsync(tournamentId));
		}

		[Fact]
		public async Task GetUserDailyScoresAsync_WithNonExistentTournament_ShouldReturnEmptyList()
		{
			// Arrange
			var tournamentId = Guid.NewGuid();
			var userId = Guid.NewGuid();
			var startDate = DateTime.UtcNow.AddDays(-5);
			var endDate = DateTime.UtcNow;

			_mockTournamentRepo.Setup(r => r.GetByIdAsync(tournamentId))
				.ReturnsAsync((Tournament?)null);

			// Act
			var result = await _tournamentService.GetUserDailyScoresAsync(tournamentId, userId, startDate, endDate);

			// Assert
			result.Should().NotBeNull();
			result.Should().BeEmpty();
		}

		[Fact]
		public async Task GetUserDailyScoresAsync_WithNonParticipant_ShouldReturnEmptyList()
		{
			// Arrange
			var tournamentId = Guid.NewGuid();
			var userId = Guid.NewGuid();
			var startDate = DateTime.UtcNow.AddDays(-5);
			var endDate = DateTime.UtcNow;

			var tournament = new Tournament
			{
				Id = tournamentId,
				StartDate = DateTime.UtcNow.AddDays(-10),
				EndDate = DateTime.UtcNow.AddDays(10),
				CreatedAt = DateTime.UtcNow
			};

			_mockTournamentRepo.Setup(r => r.GetByIdAsync(tournamentId))
				.ReturnsAsync(tournament);
			_mockParticipantRepo.Setup(r => r.GetByTournamentAsync(tournamentId))
				.ReturnsAsync(new List<TournamentParticipant>()); // No participants

			// Act
			var result = await _tournamentService.GetUserDailyScoresAsync(tournamentId, userId, startDate, endDate);

			// Assert
			result.Should().NotBeNull();
			result.Should().BeEmpty();
		}
	}
}

