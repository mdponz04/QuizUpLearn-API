using AutoMapper;
using BusinessLogic.DTOs;
using BusinessLogic.DTOs.GrammarDtos;
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
    public class GrammarServiceTest : BaseServiceTest
    {
        private readonly Mock<IGrammarRepo> _mockGrammarRepo;
        private readonly IMapper _mapper;
        private readonly GrammarService _grammarService;

        public GrammarServiceTest()
        {
            _mockGrammarRepo = new Mock<IGrammarRepo>();

            var mapperConfig = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<MappingProfile>();
            }, new NullLoggerFactory());
            _mapper = mapperConfig.CreateMapper();

            _grammarService = new GrammarService(_mockGrammarRepo.Object, _mapper);
        }

        [Fact]
        public async Task GetAllAsync_WithValidPagination_ShouldReturnPaginatedResponse()
        {
            // Arrange
            var pagination = new PaginationRequestDto
            {
                Page = 1,
                PageSize = 10,
                SearchTerm = null,
                SortBy = "Name",
                SortDirection = "asc"
            };

            var grammars = new List<Grammar>
            {
                new Grammar
                {
                    Id = Guid.NewGuid(),
                    Name = "Present Simple",
                    Tense = "Present",
                    GrammarDifficulty = GrammarDifficultyEnum.easy,
                    CreatedAt = DateTime.UtcNow
                },
                new Grammar
                {
                    Id = Guid.NewGuid(),
                    Name = "Past Perfect",
                    Tense = "Past",
                    GrammarDifficulty = GrammarDifficultyEnum.medium,
                    CreatedAt = DateTime.UtcNow
                },
                new Grammar
                {
                    Id = Guid.NewGuid(),
                    Name = "Future Continuous",
                    Tense = "Future",
                    GrammarDifficulty = GrammarDifficultyEnum.hard,
                    CreatedAt = DateTime.UtcNow
                }
            };

            _mockGrammarRepo.Setup(r => r.GetAllAsync())
                .ReturnsAsync(grammars);

            // Act
            var result = await _grammarService.GetAllAsync(pagination);

            // Assert
            result.Should().NotBeNull();
            result.Data.Should().NotBeNull();
            result.Data.Should().HaveCount(3);
            result.Pagination.Should().NotBeNull();
            result.Pagination.TotalCount.Should().Be(3);
            result.Pagination.CurrentPage.Should().Be(1);
            result.Pagination.PageSize.Should().Be(10);
            result.Pagination.TotalPages.Should().Be(1);

            // Verify all returned items are properly mapped
            result.Data.Should().AllSatisfy(item =>
            {
                item.Id.Should().NotBeEmpty();
                item.Name.Should().NotBeNullOrEmpty();
                item.GrammarDifficulty.Should().BeDefined();
                item.CreatedAt.Should().NotBe(default(DateTime));
            });

            _mockGrammarRepo.Verify(r => r.GetAllAsync(), Times.Once);
        }

        [Fact]
        public async Task GetByIdAsync_WithValidId_ShouldReturnResponseGrammarDto()
        {
            // Arrange
            var grammarId = Guid.NewGuid();
            var grammar = new Grammar
            {
                Id = grammarId,
                Name = "Present Perfect",
                Tense = "Present",
                GrammarDifficulty = GrammarDifficultyEnum.medium,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow.AddHours(-2)
            };

            _mockGrammarRepo.Setup(r => r.GetByIdAsync(grammarId))
                .ReturnsAsync(grammar);

            // Act
            var result = await _grammarService.GetByIdAsync(grammarId);

            // Assert
            result.Should().NotBeNull();
            result!.Id.Should().Be(grammarId);
            result.Name.Should().Be("Present Perfect");
            result.Tense.Should().Be("Present");
            result.GrammarDifficulty.Should().Be(GrammarDifficultyEnum.medium);
            result.CreatedAt.Should().Be(grammar.CreatedAt);
            result.UpdatedAt.Should().Be(grammar.UpdatedAt);

            _mockGrammarRepo.Verify(r => r.GetByIdAsync(grammarId), Times.Once);
        }

        [Fact]
        public async Task CreateAsync_WithValidRequest_ShouldReturnResponseGrammarDto()
        {
            // Arrange
            var request = new RequestGrammarDto
            {
                Name = "Present Continuous",
                Tense = "Present",
                GrammarDifficulty = GrammarDifficultyEnum.easy
            };

            var createdGrammar = new Grammar
            {
                Id = Guid.NewGuid(),
                Name = request.Name,
                Tense = request.Tense,
                GrammarDifficulty = request.GrammarDifficulty,
                CreatedAt = DateTime.UtcNow
            };

            _mockGrammarRepo.Setup(r => r.ExistsByNameAsync(request.Name, null))
                .ReturnsAsync(false);
            _mockGrammarRepo.Setup(r => r.CreateAsync(It.IsAny<Grammar>()))
                .ReturnsAsync(createdGrammar);

            // Act
            var result = await _grammarService.CreateAsync(request);

            // Assert
            result.Should().NotBeNull();
            result!.Id.Should().Be(createdGrammar.Id);
            result.Name.Should().Be(request.Name);
            result.Tense.Should().Be(request.Tense);
            result.GrammarDifficulty.Should().Be(request.GrammarDifficulty);
            result.CreatedAt.Should().Be(createdGrammar.CreatedAt);
            result.UpdatedAt.Should().BeNull();

            _mockGrammarRepo.Verify(r => r.ExistsByNameAsync(request.Name, null), Times.Once);
            _mockGrammarRepo.Verify(r => r.CreateAsync(It.Is<Grammar>(g =>
                g.Name == request.Name &&
                g.Tense == request.Tense &&
                g.GrammarDifficulty == request.GrammarDifficulty)), Times.Once);
        }

        [Fact]
        public async Task CreateAsync_WithNullTense_ShouldReturnResponseGrammarDto()
        {
            // Arrange
            var request = new RequestGrammarDto
            {
                Name = "Modal Verbs",
                Tense = null,
                GrammarDifficulty = GrammarDifficultyEnum.hard
            };

            var createdGrammar = new Grammar
            {
                Id = Guid.NewGuid(),
                Name = request.Name,
                Tense = request.Tense,
                GrammarDifficulty = request.GrammarDifficulty,
                CreatedAt = DateTime.UtcNow
            };

            _mockGrammarRepo.Setup(r => r.ExistsByNameAsync(request.Name, null))
                .ReturnsAsync(false);
            _mockGrammarRepo.Setup(r => r.CreateAsync(It.IsAny<Grammar>()))
                .ReturnsAsync(createdGrammar);

            // Act
            var result = await _grammarService.CreateAsync(request);

            // Assert
            result.Should().NotBeNull();
            result!.Id.Should().Be(createdGrammar.Id);
            result.Name.Should().Be(request.Name);
            result.Tense.Should().BeNull();
            result.GrammarDifficulty.Should().Be(request.GrammarDifficulty);
            result.CreatedAt.Should().Be(createdGrammar.CreatedAt);

            _mockGrammarRepo.Verify(r => r.ExistsByNameAsync(request.Name, null), Times.Once);
            _mockGrammarRepo.Verify(r => r.CreateAsync(It.IsAny<Grammar>()), Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_WithValidData_ShouldReturnUpdatedResponseGrammarDto()
        {
            // Arrange
            var grammarId = Guid.NewGuid();
            var request = new RequestGrammarDto
            {
                Name = "Updated Grammar Rule",
                Tense = "Future",
                GrammarDifficulty = GrammarDifficultyEnum.hard
            };

            var updatedGrammar = new Grammar
            {
                Id = grammarId,
                Name = request.Name,
                Tense = request.Tense,
                GrammarDifficulty = request.GrammarDifficulty,
                CreatedAt = DateTime.UtcNow.AddDays(-5),
                UpdatedAt = DateTime.UtcNow
            };

            _mockGrammarRepo.Setup(r => r.ExistsByNameAsync(request.Name, grammarId))
                .ReturnsAsync(false);
            _mockGrammarRepo.Setup(r => r.UpdateAsync(grammarId, It.IsAny<Grammar>()))
                .ReturnsAsync(updatedGrammar);

            // Act
            var result = await _grammarService.UpdateAsync(grammarId, request);

            // Assert
            result.Should().NotBeNull();
            result!.Id.Should().Be(grammarId);
            result.Name.Should().Be(request.Name);
            result.Tense.Should().Be(request.Tense);
            result.GrammarDifficulty.Should().Be(request.GrammarDifficulty);
            result.CreatedAt.Should().Be(updatedGrammar.CreatedAt);
            result.UpdatedAt.Should().Be(updatedGrammar.UpdatedAt);

            _mockGrammarRepo.Verify(r => r.ExistsByNameAsync(request.Name, grammarId), Times.Once);
            _mockGrammarRepo.Verify(r => r.UpdateAsync(grammarId, It.Is<Grammar>(g =>
                g.Name == request.Name &&
                g.Tense == request.Tense &&
                g.GrammarDifficulty == request.GrammarDifficulty)), Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_WithDifferentDifficultyLevel_ShouldReturnUpdatedResponseGrammarDto()
        {
            // Arrange
            var grammarId = Guid.NewGuid();
            var request = new RequestGrammarDto
            {
                Name = "Subjunctive Mood",
                Tense = "Various",
                GrammarDifficulty = GrammarDifficultyEnum.hard
            };

            var updatedGrammar = new Grammar
            {
                Id = grammarId,
                Name = request.Name,
                Tense = request.Tense,
                GrammarDifficulty = request.GrammarDifficulty,
                CreatedAt = DateTime.UtcNow.AddDays(-10),
                UpdatedAt = DateTime.UtcNow
            };

            _mockGrammarRepo.Setup(r => r.ExistsByNameAsync(request.Name, grammarId))
                .ReturnsAsync(false);
            _mockGrammarRepo.Setup(r => r.UpdateAsync(grammarId, It.IsAny<Grammar>()))
                .ReturnsAsync(updatedGrammar);

            // Act
            var result = await _grammarService.UpdateAsync(grammarId, request);

            // Assert
            result.Should().NotBeNull();
            result!.GrammarDifficulty.Should().Be(GrammarDifficultyEnum.hard);
            result.Name.Should().Be("Subjunctive Mood");
            result.Tense.Should().Be("Various");

            _mockGrammarRepo.Verify(r => r.UpdateAsync(grammarId, It.IsAny<Grammar>()), Times.Once);
        }

        [Fact]
        public async Task DeleteAsync_WithValidId_ShouldReturnTrue()
        {
            // Arrange
            var grammarId = Guid.NewGuid();

            _mockGrammarRepo.Setup(r => r.HasQuizzesAsync(grammarId))
                .ReturnsAsync(false);
            _mockGrammarRepo.Setup(r => r.DeleteAsync(grammarId))
                .ReturnsAsync(true);

            // Act
            var result = await _grammarService.DeleteAsync(grammarId);

            // Assert
            result.Should().BeTrue();

            _mockGrammarRepo.Verify(r => r.HasQuizzesAsync(grammarId), Times.Once);
            _mockGrammarRepo.Verify(r => r.DeleteAsync(grammarId), Times.Once);
        }

        [Fact]
        public async Task GetAllAsync_WithPaginationAndSearchTerm_ShouldReturnFilteredResults()
        {
            // Arrange
            var pagination = new PaginationRequestDto
            {
                Page = 1,
                PageSize = 5,
                SearchTerm = "present",
                SortBy = "CreatedAt",
                SortDirection = "desc"
            };

            var grammars = new List<Grammar>
            {
                new Grammar
                {
                    Id = Guid.NewGuid(),
                    Name = "Present Simple",
                    Tense = "Present",
                    GrammarDifficulty = GrammarDifficultyEnum.easy,
                    CreatedAt = DateTime.UtcNow.AddDays(-1)
                },
                new Grammar
                {
                    Id = Guid.NewGuid(),
                    Name = "Present Perfect",
                    Tense = "Present",
                    GrammarDifficulty = GrammarDifficultyEnum.medium,
                    CreatedAt = DateTime.UtcNow
                }
            };

            _mockGrammarRepo.Setup(r => r.GetAllAsync())
                .ReturnsAsync(grammars);

            // Act
            var result = await _grammarService.GetAllAsync(pagination);

            // Assert
            result.Should().NotBeNull();
            result.Data.Should().NotBeEmpty();
            result.Pagination.TotalCount.Should().BeGreaterThan(0);
            result.Data.Should().AllSatisfy(item => 
                item.Name.Should().NotBeNullOrEmpty());

            _mockGrammarRepo.Verify(r => r.GetAllAsync(), Times.Once);
        }

        [Fact]
        public async Task GetAllAsync_WithLargePage_ShouldReturnValidPaginationResponse()
        {
            // Arrange
            var pagination = new PaginationRequestDto
            {
                Page = 2,
                PageSize = 3
            };

            var grammars = new List<Grammar>
            {
                new Grammar { Id = Guid.NewGuid(), Name = "Grammar 1", GrammarDifficulty = GrammarDifficultyEnum.easy, CreatedAt = DateTime.UtcNow },
                new Grammar { Id = Guid.NewGuid(), Name = "Grammar 2", GrammarDifficulty = GrammarDifficultyEnum.medium, CreatedAt = DateTime.UtcNow },
                new Grammar { Id = Guid.NewGuid(), Name = "Grammar 3", GrammarDifficulty = GrammarDifficultyEnum.hard, CreatedAt = DateTime.UtcNow },
                new Grammar { Id = Guid.NewGuid(), Name = "Grammar 4", GrammarDifficulty = GrammarDifficultyEnum.easy, CreatedAt = DateTime.UtcNow },
                new Grammar { Id = Guid.NewGuid(), Name = "Grammar 5", GrammarDifficulty = GrammarDifficultyEnum.medium, CreatedAt = DateTime.UtcNow }
            };

            _mockGrammarRepo.Setup(r => r.GetAllAsync())
                .ReturnsAsync(grammars);

            // Act
            var result = await _grammarService.GetAllAsync(pagination);

            // Assert
            result.Should().NotBeNull();
            result.Pagination.CurrentPage.Should().Be(2);
            result.Pagination.PageSize.Should().Be(3);
            result.Pagination.TotalCount.Should().Be(5);
            result.Pagination.TotalPages.Should().Be(2);

            _mockGrammarRepo.Verify(r => r.GetAllAsync(), Times.Once);
        }

        [Fact]
        public async Task CreateAsync_WithEasyDifficulty_ShouldReturnResponseGrammarDto()
        {
            // Arrange
            var request = new RequestGrammarDto
            {
                Name = "Articles (a, an, the)",
                Tense = null,
                GrammarDifficulty = GrammarDifficultyEnum.easy
            };

            var createdGrammar = new Grammar
            {
                Id = Guid.NewGuid(),
                Name = request.Name,
                Tense = request.Tense,
                GrammarDifficulty = request.GrammarDifficulty,
                CreatedAt = DateTime.UtcNow
            };

            _mockGrammarRepo.Setup(r => r.ExistsByNameAsync(request.Name, null))
                .ReturnsAsync(false);
            _mockGrammarRepo.Setup(r => r.CreateAsync(It.IsAny<Grammar>()))
                .ReturnsAsync(createdGrammar);

            // Act
            var result = await _grammarService.CreateAsync(request);

            // Assert
            result.Should().NotBeNull();
            result!.GrammarDifficulty.Should().Be(GrammarDifficultyEnum.easy);
            result.Name.Should().Be("Articles (a, an, the)");

            _mockGrammarRepo.Verify(r => r.CreateAsync(It.IsAny<Grammar>()), Times.Once);
        }

        [Fact]
        public async Task CreateAsync_WithMediumDifficulty_ShouldReturnResponseGrammarDto()
        {
            // Arrange
            var request = new RequestGrammarDto
            {
                Name = "Conditional Sentences",
                Tense = "Mixed",
                GrammarDifficulty = GrammarDifficultyEnum.medium
            };

            var createdGrammar = new Grammar
            {
                Id = Guid.NewGuid(),
                Name = request.Name,
                Tense = request.Tense,
                GrammarDifficulty = request.GrammarDifficulty,
                CreatedAt = DateTime.UtcNow
            };

            _mockGrammarRepo.Setup(r => r.ExistsByNameAsync(request.Name, null))
                .ReturnsAsync(false);
            _mockGrammarRepo.Setup(r => r.CreateAsync(It.IsAny<Grammar>()))
                .ReturnsAsync(createdGrammar);

            // Act
            var result = await _grammarService.CreateAsync(request);

            // Assert
            result.Should().NotBeNull();
            result!.GrammarDifficulty.Should().Be(GrammarDifficultyEnum.medium);
            result.Name.Should().Be("Conditional Sentences");
            result.Tense.Should().Be("Mixed");

            _mockGrammarRepo.Verify(r => r.CreateAsync(It.IsAny<Grammar>()), Times.Once);
        }
    }
}

