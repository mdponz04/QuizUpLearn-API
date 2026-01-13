using AutoMapper;
using BusinessLogic.DTOs;
using BusinessLogic.DTOs.VocabularyDtos;
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
    public class VocabularyServiceTest : BaseControllerTest
    {
        private readonly Mock<IVocabularyRepo> _mockVocabularyRepo;
        private readonly IMapper _mapper;
        private readonly VocabularyService _vocabularyService;

        public VocabularyServiceTest()
        {
            _mockVocabularyRepo = new Mock<IVocabularyRepo>();

            var mapperConfig = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<MappingProfile>();
            }, new NullLoggerFactory());
            _mapper = mapperConfig.CreateMapper();

            _vocabularyService = new VocabularyService(_mockVocabularyRepo.Object, _mapper);
        }

        [Fact]
        public async Task GetAllAsync_WithValidPagination_ShouldReturnPaginatedVocabularies()
        {
            // Arrange
            var pagination = new PaginationRequestDto { Page = 1, PageSize = 10 };
            var vocabularies = new List<Vocabulary>
            {
                new Vocabulary 
                { 
                    Id = Guid.NewGuid(), 
                    KeyWord = "example", 
                    VocabularyDifficulty = VocabularyDifficultyEnum.easy,
                    ToeicPart = "Part 1",
                    PassageType = "Reading",
                    CreatedAt = DateTime.UtcNow,
                    Quizzes = new List<Quiz>()
                },
                new Vocabulary 
                { 
                    Id = Guid.NewGuid(), 
                    KeyWord = "complex", 
                    VocabularyDifficulty = VocabularyDifficultyEnum.hard,
                    ToeicPart = "Part 7",
                    PassageType = "Comprehension",
                    CreatedAt = DateTime.UtcNow,
                    Quizzes = new List<Quiz>()
                }
            };

            _mockVocabularyRepo.Setup(r => r.GetAllAsync())
                .ReturnsAsync(vocabularies);

            // Act
            var result = await _vocabularyService.GetAllAsync(pagination);

            // Assert
            result.Should().NotBeNull();
            result.Data.Should().HaveCount(2);
            result.Data[0].KeyWord.Should().Be("example");
            result.Data[0].VocabularyDifficulty.Should().Be(VocabularyDifficultyEnum.easy);
            result.Data[0].ToeicPart.Should().Be("Part 1");
            result.Data[0].PassageType.Should().Be("Reading");
            result.Data[1].KeyWord.Should().Be("complex");
            result.Data[1].VocabularyDifficulty.Should().Be(VocabularyDifficultyEnum.hard);
            result.Data[1].ToeicPart.Should().Be("Part 7");
            result.Data[1].PassageType.Should().Be("Comprehension");
            result.Pagination.TotalCount.Should().Be(2);

            _mockVocabularyRepo.Verify(r => r.GetAllAsync(), Times.Once);
        }

        [Fact]
        public async Task GetAllAsync_WithDifficultyFilter_ShouldReturnFilteredVocabularies()
        {
            // Arrange
            var pagination = new PaginationRequestDto { Page = 1, PageSize = 10 };
            var vocabularies = new List<Vocabulary>
            {
                new Vocabulary 
                { 
                    Id = Guid.NewGuid(), 
                    KeyWord = "easy", 
                    VocabularyDifficulty = VocabularyDifficultyEnum.easy,
                    ToeicPart = "Part 1",
                    CreatedAt = DateTime.UtcNow,
                    Quizzes = new List<Quiz>()
                },
                new Vocabulary 
                { 
                    Id = Guid.NewGuid(), 
                    KeyWord = "difficult", 
                    VocabularyDifficulty = VocabularyDifficultyEnum.hard,
                    ToeicPart = "Part 7",
                    CreatedAt = DateTime.UtcNow,
                    Quizzes = new List<Quiz>()
                }
            };

            _mockVocabularyRepo.Setup(r => r.GetAllAsync())
                .ReturnsAsync(vocabularies);

            // Act
            var result = await _vocabularyService.GetAllAsync(pagination, VocabularyDifficultyEnum.easy);

            // Assert
            result.Should().NotBeNull();
            result.Data.Should().HaveCount(1);
            result.Data[0].KeyWord.Should().Be("easy");
            result.Data[0].VocabularyDifficulty.Should().Be(VocabularyDifficultyEnum.easy);

            _mockVocabularyRepo.Verify(r => r.GetAllAsync(), Times.Once);
        }

        [Fact]
        public async Task GetAllAsync_WithEmptyResult_ShouldReturnEmptyPaginatedList()
        {
            // Arrange
            var pagination = new PaginationRequestDto { Page = 1, PageSize = 10 };
            var emptyVocabularies = new List<Vocabulary>();

            _mockVocabularyRepo.Setup(r => r.GetAllAsync())
                .ReturnsAsync(emptyVocabularies);

            // Act
            var result = await _vocabularyService.GetAllAsync(pagination);

            // Assert
            result.Should().NotBeNull();
            result.Data.Should().BeEmpty();
            result.Pagination.TotalCount.Should().Be(0);

            _mockVocabularyRepo.Verify(r => r.GetAllAsync(), Times.Once);
        }

        [Fact]
        public async Task GetByIdAsync_WithValidId_ShouldReturnVocabulary()
        {
            // Arrange
            var vocabularyId = Guid.NewGuid();
            var vocabulary = new Vocabulary
            {
                Id = vocabularyId,
                KeyWord = "achievement",
                VocabularyDifficulty = VocabularyDifficultyEnum.medium,
                ToeicPart = "Part 5",
                PassageType = "Grammar",
                CreatedAt = DateTime.UtcNow,
                Quizzes = new List<Quiz>()
            };

            _mockVocabularyRepo.Setup(r => r.GetByIdAsync(vocabularyId))
                .ReturnsAsync(vocabulary);

            // Act
            var result = await _vocabularyService.GetByIdAsync(vocabularyId);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().Be(vocabularyId);
            result.KeyWord.Should().Be("achievement");
            result.VocabularyDifficulty.Should().Be(VocabularyDifficultyEnum.medium);
            result.ToeicPart.Should().Be("Part 5");
            result.PassageType.Should().Be("Grammar");

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
        public async Task CreateAsync_WithValidRequest_ShouldReturnCreatedVocabulary()
        {
            // Arrange
            var request = new RequestVocabularyDto
            {
                KeyWord = "innovation",
                VocabularyDifficulty = VocabularyDifficultyEnum.hard,
                ToeicPart = "Part 6",
                PassageType = "Business"
            };

            var createdVocabulary = new Vocabulary
            {
                Id = Guid.NewGuid(),
                KeyWord = request.KeyWord,
                VocabularyDifficulty = request.VocabularyDifficulty,
                ToeicPart = request.ToeicPart,
                PassageType = request.PassageType,
                CreatedAt = DateTime.UtcNow,
                Quizzes = new List<Quiz>()
            };

            _mockVocabularyRepo.Setup(r => r.ExistsByKeyWordAndPartAsync(request.KeyWord, request.ToeicPart, null))
                .ReturnsAsync(false);

            _mockVocabularyRepo.Setup(r => r.CreateAsync(It.IsAny<Vocabulary>()))
                .ReturnsAsync(createdVocabulary);

            // Act
            var result = await _vocabularyService.CreateAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.KeyWord.Should().Be(request.KeyWord);
            result.VocabularyDifficulty.Should().Be(request.VocabularyDifficulty);
            result.ToeicPart.Should().Be(request.ToeicPart);
            result.PassageType.Should().Be(request.PassageType);

            _mockVocabularyRepo.Verify(r => r.ExistsByKeyWordAndPartAsync(request.KeyWord, request.ToeicPart, null), Times.Once);
            _mockVocabularyRepo.Verify(r => r.CreateAsync(It.Is<Vocabulary>(v =>
                v.KeyWord == request.KeyWord &&
                v.VocabularyDifficulty == request.VocabularyDifficulty &&
                v.ToeicPart == request.ToeicPart &&
                v.PassageType == request.PassageType)), Times.Once);
        }

        [Fact]
        public async Task CreateAsync_WithMinimalData_ShouldReturnCreatedVocabulary()
        {
            // Arrange
            var request = new RequestVocabularyDto
            {
                KeyWord = "simple",
                VocabularyDifficulty = VocabularyDifficultyEnum.easy
                // ToeicPart and PassageType are optional
            };

            var createdVocabulary = new Vocabulary
            {
                Id = Guid.NewGuid(),
                KeyWord = request.KeyWord,
                VocabularyDifficulty = request.VocabularyDifficulty,
                ToeicPart = null,
                PassageType = null,
                CreatedAt = DateTime.UtcNow,
                Quizzes = new List<Quiz>()
            };

            _mockVocabularyRepo.Setup(r => r.ExistsByKeyWordAndPartAsync(request.KeyWord, request.ToeicPart, null))
                .ReturnsAsync(false);

            _mockVocabularyRepo.Setup(r => r.CreateAsync(It.IsAny<Vocabulary>()))
                .ReturnsAsync(createdVocabulary);

            // Act
            var result = await _vocabularyService.CreateAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.KeyWord.Should().Be(request.KeyWord);
            result.VocabularyDifficulty.Should().Be(request.VocabularyDifficulty);
            result.ToeicPart.Should().BeNull();
            result.PassageType.Should().BeNull();

            _mockVocabularyRepo.Verify(r => r.ExistsByKeyWordAndPartAsync(request.KeyWord, request.ToeicPart, null), Times.Once);
            _mockVocabularyRepo.Verify(r => r.CreateAsync(It.IsAny<Vocabulary>()), Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_WithValidData_ShouldReturnUpdatedVocabulary()
        {
            // Arrange
            var vocabularyId = Guid.NewGuid();
            var request = new RequestVocabularyDto
            {
                KeyWord = "updated",
                VocabularyDifficulty = VocabularyDifficultyEnum.medium,
                ToeicPart = "Part 3",
                PassageType = "Listening"
            };

            var updatedVocabulary = new Vocabulary
            {
                Id = vocabularyId,
                KeyWord = request.KeyWord,
                VocabularyDifficulty = request.VocabularyDifficulty,
                ToeicPart = request.ToeicPart,
                PassageType = request.PassageType,
                CreatedAt = DateTime.UtcNow.AddDays(-1),
                UpdatedAt = DateTime.UtcNow,
                Quizzes = new List<Quiz>()
            };

            _mockVocabularyRepo.Setup(r => r.ExistsByKeyWordAndPartAsync(request.KeyWord, request.ToeicPart, vocabularyId))
                .ReturnsAsync(false);

            _mockVocabularyRepo.Setup(r => r.UpdateAsync(vocabularyId, It.IsAny<Vocabulary>()))
                .ReturnsAsync(updatedVocabulary);

            // Act
            var result = await _vocabularyService.UpdateAsync(vocabularyId, request);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().Be(vocabularyId);
            result.KeyWord.Should().Be(request.KeyWord);
            result.VocabularyDifficulty.Should().Be(request.VocabularyDifficulty);
            result.ToeicPart.Should().Be(request.ToeicPart);
            result.PassageType.Should().Be(request.PassageType);
            result.UpdatedAt.Should().NotBeNull();

            _mockVocabularyRepo.Verify(r => r.ExistsByKeyWordAndPartAsync(request.KeyWord, request.ToeicPart, vocabularyId), Times.Once);
            _mockVocabularyRepo.Verify(r => r.UpdateAsync(vocabularyId, It.Is<Vocabulary>(v =>
                v.KeyWord == request.KeyWord &&
                v.VocabularyDifficulty == request.VocabularyDifficulty &&
                v.ToeicPart == request.ToeicPart &&
                v.PassageType == request.PassageType)), Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_WithPartialData_ShouldReturnUpdatedVocabulary()
        {
            // Arrange
            var vocabularyId = Guid.NewGuid();
            var request = new RequestVocabularyDto
            {
                KeyWord = "partial",
                VocabularyDifficulty = VocabularyDifficultyEnum.easy
                // ToeicPart and PassageType left null
            };

            var updatedVocabulary = new Vocabulary
            {
                Id = vocabularyId,
                KeyWord = request.KeyWord,
                VocabularyDifficulty = request.VocabularyDifficulty,
                ToeicPart = null,
                PassageType = null,
                CreatedAt = DateTime.UtcNow.AddHours(-2),
                UpdatedAt = DateTime.UtcNow,
                Quizzes = new List<Quiz>()
            };

            _mockVocabularyRepo.Setup(r => r.ExistsByKeyWordAndPartAsync(request.KeyWord, request.ToeicPart, vocabularyId))
                .ReturnsAsync(false);

            _mockVocabularyRepo.Setup(r => r.UpdateAsync(vocabularyId, It.IsAny<Vocabulary>()))
                .ReturnsAsync(updatedVocabulary);

            // Act
            var result = await _vocabularyService.UpdateAsync(vocabularyId, request);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().Be(vocabularyId);
            result.KeyWord.Should().Be(request.KeyWord);
            result.VocabularyDifficulty.Should().Be(request.VocabularyDifficulty);
            result.ToeicPart.Should().BeNull();
            result.PassageType.Should().BeNull();

            _mockVocabularyRepo.Verify(r => r.ExistsByKeyWordAndPartAsync(request.KeyWord, request.ToeicPart, vocabularyId), Times.Once);
            _mockVocabularyRepo.Verify(r => r.UpdateAsync(vocabularyId, It.IsAny<Vocabulary>()), Times.Once);
        }

        [Fact]
        public async Task DeleteAsync_WithValidId_ShouldReturnTrue()
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
        public async Task DeleteAsync_WithValidIdButNoQuizzes_ShouldReturnTrue()
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
    }
}

