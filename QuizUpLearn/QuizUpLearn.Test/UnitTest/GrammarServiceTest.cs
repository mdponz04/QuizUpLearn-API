using AutoMapper;
using BusinessLogic.DTOs;
using BusinessLogic.DTOs.GrammarDtos;
using BusinessLogic.Extensions;
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
    public class GrammarServiceTest : BaseServiceTest
    {
        private readonly Mock<IGrammarRepo> _mockGrammarRepo;
        private readonly IMapper _mapper;
        private readonly GrammarService _grammarService;

        public GrammarServiceTest()
        {
            _mockGrammarRepo = new Mock<IGrammarRepo>();

            // Setup real AutoMapper with the actual mapping profile
            var mapperConfig = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<MappingProfile>();
            }, new NullLoggerFactory());
            _mapper = mapperConfig.CreateMapper();

            _grammarService = new GrammarService(
                _mockGrammarRepo.Object,
                _mapper);
        }

        [Fact]
        public async Task GetAllAsync_WithValidPagination_ShouldReturnPagedResponse()
        {
            // Arrange
            var pagination = new PaginationRequestDto
            {
                Page = 1,
                PageSize = 10
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
                }
            };

            _mockGrammarRepo.Setup(r => r.GetAllAsync())
                .ReturnsAsync(grammars);

            // Act
            var result = await _grammarService.GetAllAsync(pagination);

            // Assert
            result.Should().NotBeNull();
            result.Data.Should().NotBeNull();
            result.Pagination.TotalCount.Should().Be(2);
            result.Pagination.CurrentPage.Should().Be(1);
            result.Pagination.PageSize.Should().Be(10);

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
                Name = "Present Simple",
                Tense = "Present",
                GrammarDifficulty = GrammarDifficultyEnum.easy,
                CreatedAt = DateTime.UtcNow
            };

            _mockGrammarRepo.Setup(r => r.GetByIdAsync(grammarId))
                .ReturnsAsync(grammar);

            // Act
            var result = await _grammarService.GetByIdAsync(grammarId);

            // Assert
            result.Should().NotBeNull();
            result!.Id.Should().Be(grammarId);
            result.Name.Should().Be(grammar.Name);
            result.Tense.Should().Be(grammar.Tense);
            result.GrammarDifficulty.Should().Be(grammar.GrammarDifficulty);

            _mockGrammarRepo.Verify(r => r.GetByIdAsync(grammarId), Times.Once);
        }

        [Fact]
        public async Task GetByIdAsync_WithNonExistentId_ShouldReturnNull()
        {
            // Arrange
            var grammarId = Guid.NewGuid();
            _mockGrammarRepo.Setup(r => r.GetByIdAsync(grammarId))
                .ReturnsAsync((Grammar?)null);

            // Act
            var result = await _grammarService.GetByIdAsync(grammarId);

            // Assert
            result.Should().BeNull();
            _mockGrammarRepo.Verify(r => r.GetByIdAsync(grammarId), Times.Once);
        }

        [Fact]
        public async Task CreateAsync_WithValidData_ShouldReturnResponseGrammarDto()
        {
            // Arrange
            var requestDto = new RequestGrammarDto
            {
                Name = "Present Simple",
                Tense = "Present",
                GrammarDifficulty = GrammarDifficultyEnum.easy
            };

            var createdGrammar = new Grammar
            {
                Id = Guid.NewGuid(),
                Name = requestDto.Name,
                Tense = requestDto.Tense,
                GrammarDifficulty = requestDto.GrammarDifficulty,
                CreatedAt = DateTime.UtcNow
            };

            _mockGrammarRepo.Setup(r => r.ExistsByNameAsync(requestDto.Name, null))
                .ReturnsAsync(false);
            _mockGrammarRepo.Setup(r => r.CreateAsync(It.IsAny<Grammar>()))
                .ReturnsAsync(createdGrammar);

            // Act
            var result = await _grammarService.CreateAsync(requestDto);

            // Assert
            result.Should().NotBeNull();
            result!.Name.Should().Be(requestDto.Name);
            result.Tense.Should().Be(requestDto.Tense);
            result.GrammarDifficulty.Should().Be(requestDto.GrammarDifficulty);

            _mockGrammarRepo.Verify(r => r.ExistsByNameAsync(requestDto.Name, null), Times.Once);
            _mockGrammarRepo.Verify(r => r.CreateAsync(It.Is<Grammar>(g =>
                g.Name == requestDto.Name &&
                g.GrammarDifficulty == requestDto.GrammarDifficulty)), Times.Once);
        }

        [Fact]
        public async Task CreateAsync_WithDuplicateName_ShouldThrowException()
        {
            // Arrange
            var requestDto = new RequestGrammarDto
            {
                Name = "Present Simple",
                Tense = "Present",
                GrammarDifficulty = GrammarDifficultyEnum.easy
            };

            _mockGrammarRepo.Setup(r => r.ExistsByNameAsync(requestDto.Name, null))
                .ReturnsAsync(true);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await _grammarService.CreateAsync(requestDto));

            _mockGrammarRepo.Verify(r => r.CreateAsync(It.IsAny<Grammar>()), Times.Never);
        }

        [Fact]
        public async Task UpdateAsync_WithValidData_ShouldReturnUpdatedResponseGrammarDto()
        {
            // Arrange
            var grammarId = Guid.NewGuid();
            var requestDto = new RequestGrammarDto
            {
                Name = "Updated Grammar",
                Tense = "Future",
                GrammarDifficulty = GrammarDifficultyEnum.hard
            };

            var updatedGrammar = new Grammar
            {
                Id = grammarId,
                Name = requestDto.Name,
                Tense = requestDto.Tense,
                GrammarDifficulty = requestDto.GrammarDifficulty,
                CreatedAt = DateTime.UtcNow.AddDays(-10),
                UpdatedAt = DateTime.UtcNow
            };

            _mockGrammarRepo.Setup(r => r.ExistsByNameAsync(requestDto.Name, grammarId))
                .ReturnsAsync(false);
            _mockGrammarRepo.Setup(r => r.UpdateAsync(grammarId, It.IsAny<Grammar>()))
                .ReturnsAsync(updatedGrammar);

            // Act
            var result = await _grammarService.UpdateAsync(grammarId, requestDto);

            // Assert
            result.Should().NotBeNull();
            result!.Id.Should().Be(grammarId);
            result.Name.Should().Be(requestDto.Name);
            result.Tense.Should().Be(requestDto.Tense);
            result.GrammarDifficulty.Should().Be(requestDto.GrammarDifficulty);

            _mockGrammarRepo.Verify(r => r.ExistsByNameAsync(requestDto.Name, grammarId), Times.Once);
            _mockGrammarRepo.Verify(r => r.UpdateAsync(grammarId, It.IsAny<Grammar>()), Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_WithDuplicateName_ShouldThrowException()
        {
            // Arrange
            var grammarId = Guid.NewGuid();
            var requestDto = new RequestGrammarDto
            {
                Name = "Present Simple",
                Tense = "Present",
                GrammarDifficulty = GrammarDifficultyEnum.easy
            };

            _mockGrammarRepo.Setup(r => r.ExistsByNameAsync(requestDto.Name, grammarId))
                .ReturnsAsync(true);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await _grammarService.UpdateAsync(grammarId, requestDto));

            _mockGrammarRepo.Verify(r => r.UpdateAsync(It.IsAny<Guid>(), It.IsAny<Grammar>()), Times.Never);
        }

        [Fact]
        public async Task UpdateAsync_WithNonExistentId_ShouldReturnNull()
        {
            // Arrange
            var grammarId = Guid.NewGuid();
            var requestDto = new RequestGrammarDto
            {
                Name = "Present Simple",
                Tense = "Present",
                GrammarDifficulty = GrammarDifficultyEnum.easy
            };

            _mockGrammarRepo.Setup(r => r.ExistsByNameAsync(requestDto.Name, grammarId))
                .ReturnsAsync(false);
            _mockGrammarRepo.Setup(r => r.UpdateAsync(grammarId, It.IsAny<Grammar>()))
                .ReturnsAsync((Grammar?)null);

            // Act
            var result = await _grammarService.UpdateAsync(grammarId, requestDto);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task DeleteAsync_WithValidIdAndNoQuizzes_ShouldReturnTrue()
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
        public async Task DeleteAsync_WithGrammarHasQuizzes_ShouldReturnFalse()
        {
            // Arrange
            var grammarId = Guid.NewGuid();
            _mockGrammarRepo.Setup(r => r.HasQuizzesAsync(grammarId))
                .ReturnsAsync(true);

            // Act
            var result = await _grammarService.DeleteAsync(grammarId);

            // Assert
            result.Should().BeFalse();
            _mockGrammarRepo.Verify(r => r.HasQuizzesAsync(grammarId), Times.Once);
            _mockGrammarRepo.Verify(r => r.DeleteAsync(It.IsAny<Guid>()), Times.Never);
        }
    }
}

