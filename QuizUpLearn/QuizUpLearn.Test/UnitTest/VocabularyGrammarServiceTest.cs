using BusinessLogic.DTOs;
using BusinessLogic.Services;
using FluentAssertions;
using Moq;
using Repository.Entities;
using Repository.Enums;
using Repository.Interfaces;

namespace QuizUpLearn.Test.UnitTest
{
    public class VocabularyGrammarServiceTest : BaseControllerTest
    {
        private readonly Mock<IVocabularyRepo> _mockVocabularyRepo;
        private readonly Mock<IGrammarRepo> _mockGrammarRepo;
        private readonly VocabularyGrammarService _vocabularyGrammarService;

        public VocabularyGrammarServiceTest()
        {
            _mockVocabularyRepo = new Mock<IVocabularyRepo>();
            _mockGrammarRepo = new Mock<IGrammarRepo>();

            _vocabularyGrammarService = new VocabularyGrammarService(
                _mockVocabularyRepo.Object,
                _mockGrammarRepo.Object);
        }

        [Fact]
        public async Task GetUnusedPairVocabularyGrammar_WithValidPagination_ShouldReturnPagedResponse()
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
                    ToeicPart = "1",
                    CreatedAt = DateTime.UtcNow,
                    Quizzes = new List<Quiz>() // No quizzes - unused vocabulary
                },
                new Vocabulary
                {
                    Id = Guid.NewGuid(),
                    KeyWord = "example",
                    VocabularyDifficulty = VocabularyDifficultyEnum.medium,
                    ToeicPart = "2",
                    CreatedAt = DateTime.UtcNow,
                    Quizzes = new List<Quiz>()
                }
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

            _mockVocabularyRepo.Setup(r => r.GetAllAsync())
                .ReturnsAsync(vocabularies);
            _mockGrammarRepo.Setup(r => r.GetAllAsync())
                .ReturnsAsync(grammars);

            // Act
            var result = await _vocabularyGrammarService.GetUnusedPairVocabularyGrammar(pagination);

            // Assert
            result.Should().NotBeNull();
            result.Data.Should().NotBeNull();
            result.Data.Should().HaveCount(4); // 2 vocabularies × 2 grammars = 4 pairs
            result.Pagination.Should().NotBeNull();
            result.Pagination.CurrentPage.Should().Be(1);
            result.Pagination.PageSize.Should().Be(10);

            // Verify each pair has both vocabulary and grammar
            result.Data.Should().AllSatisfy(pair =>
            {
                pair.Vocabulary.Should().NotBeNull();
                pair.Grammar.Should().NotBeNull();
                pair.Part.Should().NotBeNullOrEmpty();
            });

            _mockVocabularyRepo.Verify(r => r.GetAllAsync(), Times.Once);
            _mockGrammarRepo.Verify(r => r.GetAllAsync(), Times.Once);
        }

        [Fact]
        public async Task GetUnusedPairVocabularyGrammar_WithNullPagination_ShouldUseDefaultPagination()
        {
            // Arrange
            var vocabularies = new List<Vocabulary>
            {
                new Vocabulary
                {
                    Id = Guid.NewGuid(),
                    KeyWord = "test",
                    VocabularyDifficulty = VocabularyDifficultyEnum.easy,
                    ToeicPart = "1",
                    CreatedAt = DateTime.UtcNow,
                    Quizzes = new List<Quiz>()
                }
            };

            var grammars = new List<Grammar>
            {
                new Grammar
                {
                    Id = Guid.NewGuid(),
                    Name = "Present Simple",
                    GrammarDifficulty = GrammarDifficultyEnum.easy,
                    CreatedAt = DateTime.UtcNow
                }
            };

            _mockVocabularyRepo.Setup(r => r.GetAllAsync())
                .ReturnsAsync(vocabularies);
            _mockGrammarRepo.Setup(r => r.GetAllAsync())
                .ReturnsAsync(grammars);

            // Act
            var result = await _vocabularyGrammarService.GetUnusedPairVocabularyGrammar(null);

            // Assert
            result.Should().NotBeNull();
            result.Data.Should().NotBeNull();
            result.Data.Should().HaveCount(1); // 1 vocabulary × 1 grammar = 1 pair
            result.Pagination.Should().NotBeNull();

            _mockVocabularyRepo.Verify(r => r.GetAllAsync(), Times.Once);
            _mockGrammarRepo.Verify(r => r.GetAllAsync(), Times.Once);
        }

        [Fact]
        public async Task GetUnusedPairVocabularyGrammar_WithVocabularyHasPartialQuizzes_ShouldReturnUnusedPairs()
        {
            // Arrange
            var grammarId1 = Guid.NewGuid();
            var grammarId2 = Guid.NewGuid();
            
            var vocabularies = new List<Vocabulary>
            {
                new Vocabulary
                {
                    Id = Guid.NewGuid(),
                    KeyWord = "test",
                    VocabularyDifficulty = VocabularyDifficultyEnum.easy,
                    ToeicPart = "1",
                    CreatedAt = DateTime.UtcNow,
                    Quizzes = new List<Quiz>
                    {
                        new Quiz
                        {
                            Id = Guid.NewGuid(),
                            GrammarId = grammarId1,
                            QuestionText = "Test question",
                            TOEICPart = "PART1"
                        }
                    }
                }
            };

            var grammars = new List<Grammar>
            {
                new Grammar
                {
                    Id = grammarId1,
                    Name = "Present Simple",
                    GrammarDifficulty = GrammarDifficultyEnum.easy,
                    CreatedAt = DateTime.UtcNow
                },
                new Grammar
                {
                    Id = grammarId2,
                    Name = "Past Perfect",
                    GrammarDifficulty = GrammarDifficultyEnum.medium,
                    CreatedAt = DateTime.UtcNow
                }
            };

            _mockVocabularyRepo.Setup(r => r.GetAllAsync())
                .ReturnsAsync(vocabularies);
            _mockGrammarRepo.Setup(r => r.GetAllAsync())
                .ReturnsAsync(grammars);

            // Act
            var result = await _vocabularyGrammarService.GetUnusedPairVocabularyGrammar();

            // Assert
            result.Should().NotBeNull();
            result.Data.Should().NotBeNull();
            result.Data.Should().HaveCount(1); // Only the unused grammar (grammarId2) should be paired
            result.Data.First().Grammar.Id.Should().Be(grammarId2);
            result.Data.First().Vocabulary.KeyWord.Should().Be("test");

            _mockVocabularyRepo.Verify(r => r.GetAllAsync(), Times.Once);
            _mockGrammarRepo.Verify(r => r.GetAllAsync(), Times.Once);
        }

        [Fact]
        public async Task GetUnusedPairVocabularyGrammar_WithSearchTerm_ShouldReturnFilteredResults()
        {
            // Arrange
            var pagination = new PaginationRequestDto
            {
                Page = 1,
                PageSize = 10,
                SearchTerm = "present"
            };

            var vocabularies = new List<Vocabulary>
            {
                new Vocabulary
                {
                    Id = Guid.NewGuid(),
                    KeyWord = "test",
                    VocabularyDifficulty = VocabularyDifficultyEnum.easy,
                    ToeicPart = "1",
                    CreatedAt = DateTime.UtcNow,
                    Quizzes = new List<Quiz>()
                }
            };

            var grammars = new List<Grammar>
            {
                new Grammar
                {
                    Id = Guid.NewGuid(),
                    Name = "Present Simple",
                    GrammarDifficulty = GrammarDifficultyEnum.easy,
                    CreatedAt = DateTime.UtcNow
                },
                new Grammar
                {
                    Id = Guid.NewGuid(),
                    Name = "Past Perfect",
                    GrammarDifficulty = GrammarDifficultyEnum.medium,
                    CreatedAt = DateTime.UtcNow
                }
            };

            _mockVocabularyRepo.Setup(r => r.GetAllAsync())
                .ReturnsAsync(vocabularies);
            _mockGrammarRepo.Setup(r => r.GetAllAsync())
                .ReturnsAsync(grammars);

            // Act
            var result = await _vocabularyGrammarService.GetUnusedPairVocabularyGrammar(pagination);

            // Assert
            result.Should().NotBeNull();
            result.Data.Should().NotBeNull();
            result.Data.Should().HaveCount(1); // Only grammar with "present" in name
            result.Data.First().Grammar.Name.Should().Contain("Present");

            _mockVocabularyRepo.Verify(r => r.GetAllAsync(), Times.Once);
            _mockGrammarRepo.Verify(r => r.GetAllAsync(), Times.Once);
        }

        [Fact]
        public async Task GetUnusedPairVocabularyGrammar_WithPartFilter_ShouldReturnFilteredResults()
        {
            // Arrange
            var pagination = new PaginationRequestDto
            {
                Page = 1,
                PageSize = 10,
                Filters = new Dictionary<string, object>
                {
                    { "part", "1" }
                }
            };

            var vocabularies = new List<Vocabulary>
            {
                new Vocabulary
                {
                    Id = Guid.NewGuid(),
                    KeyWord = "test",
                    VocabularyDifficulty = VocabularyDifficultyEnum.easy,
                    ToeicPart = "1",
                    CreatedAt = DateTime.UtcNow,
                    Quizzes = new List<Quiz>()
                },
                new Vocabulary
                {
                    Id = Guid.NewGuid(),
                    KeyWord = "example",
                    VocabularyDifficulty = VocabularyDifficultyEnum.medium,
                    ToeicPart = "2",
                    CreatedAt = DateTime.UtcNow,
                    Quizzes = new List<Quiz>()
                }
            };

            var grammars = new List<Grammar>
            {
                new Grammar
                {
                    Id = Guid.NewGuid(),
                    Name = "Present Simple",
                    GrammarDifficulty = GrammarDifficultyEnum.easy,
                    CreatedAt = DateTime.UtcNow
                }
            };

            _mockVocabularyRepo.Setup(r => r.GetAllAsync())
                .ReturnsAsync(vocabularies);
            _mockGrammarRepo.Setup(r => r.GetAllAsync())
                .ReturnsAsync(grammars);

            // Act
            var result = await _vocabularyGrammarService.GetUnusedPairVocabularyGrammar(pagination);

            // Assert
            result.Should().NotBeNull();
            result.Data.Should().NotBeNull();
            result.Data.Should().HaveCount(1);
            result.Data.First().Part.Should().Be("PART1");
            result.Data.First().Vocabulary.ToeicPart.Should().Be("1");

            _mockVocabularyRepo.Verify(r => r.GetAllAsync(), Times.Once);
            _mockGrammarRepo.Verify(r => r.GetAllAsync(), Times.Once);
        }

        [Fact]
        public async Task GetUnusedPairVocabularyGrammar_WithMultiplePartsVocabulary_ShouldReturnCorrectPartNames()
        {
            // Arrange
            var vocabularies = new List<Vocabulary>
            {
                new Vocabulary
                {
                    Id = Guid.NewGuid(),
                    KeyWord = "part1word",
                    VocabularyDifficulty = VocabularyDifficultyEnum.easy,
                    ToeicPart = "1",
                    CreatedAt = DateTime.UtcNow,
                    Quizzes = new List<Quiz>()
                },
                new Vocabulary
                {
                    Id = Guid.NewGuid(),
                    KeyWord = "part3word",
                    VocabularyDifficulty = VocabularyDifficultyEnum.medium,
                    ToeicPart = "3",
                    CreatedAt = DateTime.UtcNow,
                    Quizzes = new List<Quiz>()
                },
                new Vocabulary
                {
                    Id = Guid.NewGuid(),
                    KeyWord = "part7word",
                    VocabularyDifficulty = VocabularyDifficultyEnum.hard,
                    ToeicPart = "7",
                    CreatedAt = DateTime.UtcNow,
                    Quizzes = new List<Quiz>()
                }
            };

            var grammars = new List<Grammar>
            {
                new Grammar
                {
                    Id = Guid.NewGuid(),
                    Name = "Present Simple",
                    GrammarDifficulty = GrammarDifficultyEnum.easy,
                    CreatedAt = DateTime.UtcNow
                }
            };

            _mockVocabularyRepo.Setup(r => r.GetAllAsync())
                .ReturnsAsync(vocabularies);
            _mockGrammarRepo.Setup(r => r.GetAllAsync())
                .ReturnsAsync(grammars);

            // Act
            var result = await _vocabularyGrammarService.GetUnusedPairVocabularyGrammar();

            // Assert
            result.Should().NotBeNull();
            result.Data.Should().NotBeNull();
            result.Data.Should().HaveCount(3); // 3 vocabularies × 1 grammar = 3 pairs

            var part1Pair = result.Data.FirstOrDefault(p => p.Part == "PART1");
            var part3Pair = result.Data.FirstOrDefault(p => p.Part == "PART3");
            var part7Pair = result.Data.FirstOrDefault(p => p.Part == "PART7");

            part1Pair.Should().NotBeNull();
            part1Pair!.Vocabulary.KeyWord.Should().Be("part1word");
            
            part3Pair.Should().NotBeNull();
            part3Pair!.Vocabulary.KeyWord.Should().Be("part3word");
            
            part7Pair.Should().NotBeNull();
            part7Pair!.Vocabulary.KeyWord.Should().Be("part7word");

            _mockVocabularyRepo.Verify(r => r.GetAllAsync(), Times.Once);
            _mockGrammarRepo.Verify(r => r.GetAllAsync(), Times.Once);
        }

        [Fact]
        public async Task GetUnusedPairVocabularyGrammar_WithEmptyVocabulariesAndGrammars_ShouldReturnEmptyResult()
        {
            // Arrange
            _mockVocabularyRepo.Setup(r => r.GetAllAsync())
                .ReturnsAsync(new List<Vocabulary>());
            _mockGrammarRepo.Setup(r => r.GetAllAsync())
                .ReturnsAsync(new List<Grammar>());

            // Act
            var result = await _vocabularyGrammarService.GetUnusedPairVocabularyGrammar();

            // Assert
            result.Should().NotBeNull();
            result.Data.Should().NotBeNull();
            result.Data.Should().BeEmpty();
            result.Pagination.TotalCount.Should().Be(0);

            _mockVocabularyRepo.Verify(r => r.GetAllAsync(), Times.Once);
            _mockGrammarRepo.Verify(r => r.GetAllAsync(), Times.Once);
        }

        [Fact]
        public async Task GetUnusedPairVocabularyGrammar_WithVocabularySearchTerm_ShouldReturnMatchingVocabulary()
        {
            // Arrange
            var pagination = new PaginationRequestDto
            {
                Page = 1,
                PageSize = 10,
                SearchTerm = "example"
            };

            var vocabularies = new List<Vocabulary>
            {
                new Vocabulary
                {
                    Id = Guid.NewGuid(),
                    KeyWord = "test",
                    VocabularyDifficulty = VocabularyDifficultyEnum.easy,
                    ToeicPart = "1",
                    CreatedAt = DateTime.UtcNow,
                    Quizzes = new List<Quiz>()
                },
                new Vocabulary
                {
                    Id = Guid.NewGuid(),
                    KeyWord = "example",
                    VocabularyDifficulty = VocabularyDifficultyEnum.medium,
                    ToeicPart = "2",
                    CreatedAt = DateTime.UtcNow,
                    Quizzes = new List<Quiz>()
                }
            };

            var grammars = new List<Grammar>
            {
                new Grammar
                {
                    Id = Guid.NewGuid(),
                    Name = "Present Simple",
                    GrammarDifficulty = GrammarDifficultyEnum.easy,
                    CreatedAt = DateTime.UtcNow
                }
            };

            _mockVocabularyRepo.Setup(r => r.GetAllAsync())
                .ReturnsAsync(vocabularies);
            _mockGrammarRepo.Setup(r => r.GetAllAsync())
                .ReturnsAsync(grammars);

            // Act
            var result = await _vocabularyGrammarService.GetUnusedPairVocabularyGrammar(pagination);

            // Assert
            result.Should().NotBeNull();
            result.Data.Should().NotBeNull();
            result.Data.Should().HaveCount(1); // Only vocabulary with "example" in keyword
            result.Data.First().Vocabulary.KeyWord.Should().Be("example");

            _mockVocabularyRepo.Verify(r => r.GetAllAsync(), Times.Once);
            _mockGrammarRepo.Verify(r => r.GetAllAsync(), Times.Once);
        }
    }
}