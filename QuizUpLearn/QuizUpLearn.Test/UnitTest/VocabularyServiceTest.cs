using AutoMapper;
using BusinessLogic.DTOs;
using BusinessLogic.DTOs.VocabularyDtos;
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
    public class VocabularyServiceTest : BaseServiceTest
    {
        private readonly Mock<IVocabularyRepo> _mockVocabularyRepo;
        private readonly IMapper _mapper;
        private readonly VocabularyService _vocabularyService;

        public VocabularyServiceTest()
        {
            _mockVocabularyRepo = new Mock<IVocabularyRepo>();

            // Setup real AutoMapper with the actual mapping profile
            var mapperConfig = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<MappingProfile>();
            }, new NullLoggerFactory());
            _mapper = mapperConfig.CreateMapper();

            _vocabularyService = new VocabularyService(
                _mockVocabularyRepo.Object,
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

            var vocabularies = new List<Vocabulary>
            {
                new Vocabulary
                {
                    Id = Guid.NewGuid(),
                    KeyWord = "test",
                    VocabularyDifficulty = VocabularyDifficultyEnum.easy,
                    ToeicPart = "PART1",
                    CreatedAt = DateTime.UtcNow
                },
                new Vocabulary
                {
                    Id = Guid.NewGuid(),
                    KeyWord = "example",
                    VocabularyDifficulty = VocabularyDifficultyEnum.medium,
                    ToeicPart = "PART2",
                    CreatedAt = DateTime.UtcNow
                }
            };

            _mockVocabularyRepo.Setup(r => r.GetAllAsync())
                .ReturnsAsync(vocabularies);

            // Act
            var result = await _vocabularyService.GetAllAsync(pagination);

            // Assert
            result.Should().NotBeNull();
            result.Data.Should().NotBeNull();
            result.Pagination.TotalCount.Should().Be(2);
            result.Pagination.CurrentPage.Should().Be(1);
            result.Pagination.PageSize.Should().Be(10);

            _mockVocabularyRepo.Verify(r => r.GetAllAsync(), Times.Once);
        }

        [Fact]
        public async Task GetAllAsync_WithDifficultyFilter_ShouldReturnFilteredResults()
        {
            // Arrange
            var pagination = new PaginationRequestDto
            {
                Page = 1,
                PageSize = 10
            };

            var vocabularies = new List<Vocabulary>
            {
                new Vocabulary
                {
                    Id = Guid.NewGuid(),
                    KeyWord = "test",
                    VocabularyDifficulty = VocabularyDifficultyEnum.easy,
                    ToeicPart = "PART1",
                    CreatedAt = DateTime.UtcNow
                },
                new Vocabulary
                {
                    Id = Guid.NewGuid(),
                    KeyWord = "example",
                    VocabularyDifficulty = VocabularyDifficultyEnum.medium,
                    ToeicPart = "PART2",
                    CreatedAt = DateTime.UtcNow
                },
                new Vocabulary
                {
                    Id = Guid.NewGuid(),
                    KeyWord = "hardword",
                    VocabularyDifficulty = VocabularyDifficultyEnum.hard,
                    ToeicPart = "PART3",
                    CreatedAt = DateTime.UtcNow
                }
            };

            _mockVocabularyRepo.Setup(r => r.GetAllAsync())
                .ReturnsAsync(vocabularies);

            // Act
            var result = await _vocabularyService.GetAllAsync(pagination, VocabularyDifficultyEnum.easy);

            // Assert
            result.Should().NotBeNull();
            result.Data.Should().NotBeNull();
            result.Data.Should().HaveCount(1);
            result.Data.All(v => v.VocabularyDifficulty == VocabularyDifficultyEnum.easy).Should().BeTrue();

            _mockVocabularyRepo.Verify(r => r.GetAllAsync(), Times.Once);
        }

        [Fact]
        public async Task GetByIdAsync_WithValidId_ShouldReturnResponseVocabularyDto()
        {
            // Arrange
            var vocabularyId = Guid.NewGuid();
            var vocabulary = new Vocabulary
            {
                Id = vocabularyId,
                KeyWord = "test",
                VocabularyDifficulty = VocabularyDifficultyEnum.easy,
                ToeicPart = "PART1",
                PassageType = "reading",
                CreatedAt = DateTime.UtcNow
            };

            _mockVocabularyRepo.Setup(r => r.GetByIdAsync(vocabularyId))
                .ReturnsAsync(vocabulary);

            // Act
            var result = await _vocabularyService.GetByIdAsync(vocabularyId);

            // Assert
            result.Should().NotBeNull();
            result!.Id.Should().Be(vocabularyId);
            result.KeyWord.Should().Be(vocabulary.KeyWord);
            result.VocabularyDifficulty.Should().Be(vocabulary.VocabularyDifficulty);
            result.ToeicPart.Should().Be(vocabulary.ToeicPart);

            _mockVocabularyRepo.Verify(r => r.GetByIdAsync(vocabularyId), Times.Once);
        }

        [Fact]
        public async Task GetByIdAsync_WithNonExistentId_ShouldReturnNull()
        {
            // Arrange
            var vocabularyId = Guid.NewGuid();
            _mockVocabularyRepo.Setup(r => r.GetByIdAsync(vocabularyId))
                .ReturnsAsync((Vocabulary?)null);

            // Act
            var result = await _vocabularyService.GetByIdAsync(vocabularyId);

            // Assert
            result.Should().BeNull();
            _mockVocabularyRepo.Verify(r => r.GetByIdAsync(vocabularyId), Times.Once);
        }

        [Fact]
        public async Task CreateAsync_WithValidData_ShouldReturnResponseVocabularyDto()
        {
            // Arrange
            var requestDto = new RequestVocabularyDto
            {
                KeyWord = "test",
                VocabularyDifficulty = VocabularyDifficultyEnum.easy,
                ToeicPart = "PART1",
                PassageType = "reading"
            };

            var createdVocabulary = new Vocabulary
            {
                Id = Guid.NewGuid(),
                KeyWord = requestDto.KeyWord,
                VocabularyDifficulty = requestDto.VocabularyDifficulty,
                ToeicPart = requestDto.ToeicPart,
                PassageType = requestDto.PassageType,
                CreatedAt = DateTime.UtcNow
            };

            _mockVocabularyRepo.Setup(r => r.ExistsByKeyWordAndPartAsync(requestDto.KeyWord, requestDto.ToeicPart, null))
                .ReturnsAsync(false);
            _mockVocabularyRepo.Setup(r => r.CreateAsync(It.IsAny<Vocabulary>()))
                .ReturnsAsync(createdVocabulary);

            // Act
            var result = await _vocabularyService.CreateAsync(requestDto);

            // Assert
            result.Should().NotBeNull();
            result!.KeyWord.Should().Be(requestDto.KeyWord);
            result.VocabularyDifficulty.Should().Be(requestDto.VocabularyDifficulty);
            result.ToeicPart.Should().Be(requestDto.ToeicPart);

            _mockVocabularyRepo.Verify(r => r.ExistsByKeyWordAndPartAsync(requestDto.KeyWord, requestDto.ToeicPart, null), Times.Once);
            _mockVocabularyRepo.Verify(r => r.CreateAsync(It.Is<Vocabulary>(v =>
                v.KeyWord == requestDto.KeyWord &&
                v.VocabularyDifficulty == requestDto.VocabularyDifficulty)), Times.Once);
        }

        [Fact]
        public async Task CreateAsync_WithDuplicateKeyWordInSamePart_ShouldThrowException()
        {
            // Arrange
            var requestDto = new RequestVocabularyDto
            {
                KeyWord = "test",
                VocabularyDifficulty = VocabularyDifficultyEnum.easy,
                ToeicPart = "PART1"
            };

            _mockVocabularyRepo.Setup(r => r.ExistsByKeyWordAndPartAsync(requestDto.KeyWord, requestDto.ToeicPart, null))
                .ReturnsAsync(true);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await _vocabularyService.CreateAsync(requestDto));

            _mockVocabularyRepo.Verify(r => r.CreateAsync(It.IsAny<Vocabulary>()), Times.Never);
        }

        [Fact]
        public async Task CreateAsync_WithDuplicateKeyWordInNoPart_ShouldThrowException()
        {
            // Arrange
            var requestDto = new RequestVocabularyDto
            {
                KeyWord = "test",
                VocabularyDifficulty = VocabularyDifficultyEnum.easy,
                ToeicPart = null
            };

            _mockVocabularyRepo.Setup(r => r.ExistsByKeyWordAndPartAsync(requestDto.KeyWord, requestDto.ToeicPart, null))
                .ReturnsAsync(true);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await _vocabularyService.CreateAsync(requestDto));

            exception.Message.Should().Contain("no part");
            _mockVocabularyRepo.Verify(r => r.CreateAsync(It.IsAny<Vocabulary>()), Times.Never);
        }

        [Fact]
        public async Task UpdateAsync_WithValidData_ShouldReturnUpdatedResponseVocabularyDto()
        {
            // Arrange
            var vocabularyId = Guid.NewGuid();
            var requestDto = new RequestVocabularyDto
            {
                KeyWord = "updated",
                VocabularyDifficulty = VocabularyDifficultyEnum.medium,
                ToeicPart = "PART2",
                PassageType = "listening"
            };

            var updatedVocabulary = new Vocabulary
            {
                Id = vocabularyId,
                KeyWord = requestDto.KeyWord,
                VocabularyDifficulty = requestDto.VocabularyDifficulty,
                ToeicPart = requestDto.ToeicPart,
                PassageType = requestDto.PassageType,
                CreatedAt = DateTime.UtcNow.AddDays(-10),
                UpdatedAt = DateTime.UtcNow
            };

            _mockVocabularyRepo.Setup(r => r.ExistsByKeyWordAndPartAsync(requestDto.KeyWord, requestDto.ToeicPart, vocabularyId))
                .ReturnsAsync(false);
            _mockVocabularyRepo.Setup(r => r.UpdateAsync(vocabularyId, It.IsAny<Vocabulary>()))
                .ReturnsAsync(updatedVocabulary);

            // Act
            var result = await _vocabularyService.UpdateAsync(vocabularyId, requestDto);

            // Assert
            result.Should().NotBeNull();
            result!.Id.Should().Be(vocabularyId);
            result.KeyWord.Should().Be(requestDto.KeyWord);
            result.VocabularyDifficulty.Should().Be(requestDto.VocabularyDifficulty);

            _mockVocabularyRepo.Verify(r => r.ExistsByKeyWordAndPartAsync(requestDto.KeyWord, requestDto.ToeicPart, vocabularyId), Times.Once);
            _mockVocabularyRepo.Verify(r => r.UpdateAsync(vocabularyId, It.IsAny<Vocabulary>()), Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_WithDuplicateKeyWordInSamePart_ShouldThrowException()
        {
            // Arrange
            var vocabularyId = Guid.NewGuid();
            var requestDto = new RequestVocabularyDto
            {
                KeyWord = "test",
                VocabularyDifficulty = VocabularyDifficultyEnum.easy,
                ToeicPart = "PART1"
            };

            _mockVocabularyRepo.Setup(r => r.ExistsByKeyWordAndPartAsync(requestDto.KeyWord, requestDto.ToeicPart, vocabularyId))
                .ReturnsAsync(true);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await _vocabularyService.UpdateAsync(vocabularyId, requestDto));

            _mockVocabularyRepo.Verify(r => r.UpdateAsync(It.IsAny<Guid>(), It.IsAny<Vocabulary>()), Times.Never);
        }

        [Fact]
        public async Task UpdateAsync_WithNonExistentId_ShouldReturnNull()
        {
            // Arrange
            var vocabularyId = Guid.NewGuid();
            var requestDto = new RequestVocabularyDto
            {
                KeyWord = "test",
                VocabularyDifficulty = VocabularyDifficultyEnum.easy,
                ToeicPart = "PART1"
            };

            _mockVocabularyRepo.Setup(r => r.ExistsByKeyWordAndPartAsync(requestDto.KeyWord, requestDto.ToeicPart, vocabularyId))
                .ReturnsAsync(false);
            _mockVocabularyRepo.Setup(r => r.UpdateAsync(vocabularyId, It.IsAny<Vocabulary>()))
                .ReturnsAsync((Vocabulary?)null);

            // Act
            var result = await _vocabularyService.UpdateAsync(vocabularyId, requestDto);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task DeleteAsync_WithValidIdAndNoQuizzes_ShouldReturnTrue()
        {
            // Arrange
            var vocabularyId = Guid.NewGuid();
            _mockVocabularyRepo.Setup(r => r.HasQuizzesAsync(vocabularyId))
                .ReturnsAsync(false);
            _mockVocabularyRepo.Setup(r => r.DeleteAsync(vocabularyId))
                .ReturnsAsync(true);

            // Act
            var result = await _vocabularyService.DeleteAsync(vocabularyId);

            // Assert
            result.Should().BeTrue();
            _mockVocabularyRepo.Verify(r => r.HasQuizzesAsync(vocabularyId), Times.Once);
            _mockVocabularyRepo.Verify(r => r.DeleteAsync(vocabularyId), Times.Once);
        }

        [Fact]
        public async Task DeleteAsync_WithVocabularyHasQuizzes_ShouldReturnFalse()
        {
            // Arrange
            var vocabularyId = Guid.NewGuid();
            _mockVocabularyRepo.Setup(r => r.HasQuizzesAsync(vocabularyId))
                .ReturnsAsync(true);

            // Act
            var result = await _vocabularyService.DeleteAsync(vocabularyId);

            // Assert
            result.Should().BeFalse();
            _mockVocabularyRepo.Verify(r => r.HasQuizzesAsync(vocabularyId), Times.Once);
            _mockVocabularyRepo.Verify(r => r.DeleteAsync(It.IsAny<Guid>()), Times.Never);
        }
    }
}

