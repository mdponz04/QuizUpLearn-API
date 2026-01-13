using BusinessLogic.DTOs;
using BusinessLogic.DTOs.GrammarDtos;
using BusinessLogic.DTOs.VocabularyDtos;
using BusinessLogic.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using QuizUpLearn.API.Controllers;
using Repository.Enums;
using System.Security.Claims;

namespace QuizUpLearn.Test.IntegrationTest
{
    public class VocabularyGrammarControllerTest : BaseControllerTest
    {
        private readonly Mock<IVocabularyGrammarService> _mockVocabularyGrammarService;
        private readonly VocabularyGrammarController _controller;
        private static readonly List<GrammarVocabularyResponseDto> _testData = new()
        {
            new GrammarVocabularyResponseDto
            {
                Grammar = new ResponseGrammarDto
                {
                    Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                    Name = "Subjunctive Mood",
                    Tense = "Subjunctive",
                    GrammarDifficulty = GrammarDifficultyEnum.hard,
                    CreatedAt = DateTime.Parse("2024-03-15"),
                    UpdatedAt = DateTime.Parse("2024-03-20")
                },
                Vocabulary = new ResponseVocabularyDto
                {
                    Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                    KeyWord = "xenophobia",
                    VocabularyDifficulty = VocabularyDifficultyEnum.hard,
                    ToeicPart = "7",
                    PassageType = "Reading",
                    CreatedAt = DateTime.Parse("2024-03-10"),
                    UpdatedAt = DateTime.Parse("2024-03-15")
                },
                Part = "PART7"
            },
            new GrammarVocabularyResponseDto
            {
                Grammar = new ResponseGrammarDto
                {
                    Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                    Name = "Infinitive Construction",
                    Tense = "Infinitive",
                    GrammarDifficulty = GrammarDifficultyEnum.medium,
                    CreatedAt = DateTime.Parse("2024-02-20"),
                    UpdatedAt = null
                },
                Vocabulary = new ResponseVocabularyDto
                {
                    Id = Guid.Parse("44444444-4444-4444-4444-444444444444"),
                    KeyWord = "bibliophile",
                    VocabularyDifficulty = VocabularyDifficultyEnum.medium,
                    ToeicPart = "5",
                    PassageType = "Grammar",
                    CreatedAt = DateTime.Parse("2024-02-25"),
                    UpdatedAt = DateTime.Parse("2024-03-01")
                },
                Part = "PART5"
            },
            new GrammarVocabularyResponseDto
            {
                Grammar = new ResponseGrammarDto
                {
                    Id = Guid.Parse("55555555-5555-5555-5555-555555555555"),
                    Name = "Gerund Phrase",
                    Tense = "Gerund",
                    GrammarDifficulty = GrammarDifficultyEnum.easy,
                    CreatedAt = DateTime.Parse("2024-01-10"),
                    UpdatedAt = DateTime.Parse("2024-01-25")
                },
                Vocabulary = new ResponseVocabularyDto
                {
                    Id = Guid.Parse("66666666-6666-6666-6666-666666666666"),
                    KeyWord = "quixotic",
                    VocabularyDifficulty = VocabularyDifficultyEnum.hard,
                    ToeicPart = "3",
                    PassageType = "Listening",
                    CreatedAt = DateTime.Parse("2024-01-05"),
                    UpdatedAt = null
                },
                Part = "PART3"
            },
            new GrammarVocabularyResponseDto
            {
                Grammar = new ResponseGrammarDto
                {
                    Id = Guid.Parse("77777777-7777-7777-7777-777777777777"),
                    Name = "Conditional Perfect",
                    Tense = "Conditional",
                    GrammarDifficulty = GrammarDifficultyEnum.hard,
                    CreatedAt = DateTime.Parse("2024-04-01"),
                    UpdatedAt = DateTime.Parse("2024-04-10")
                },
                Vocabulary = new ResponseVocabularyDto
                {
                    Id = Guid.Parse("88888888-8888-8888-8888-888888888888"),
                    KeyWord = "zephyr",
                    VocabularyDifficulty = VocabularyDifficultyEnum.easy,
                    ToeicPart = "1",
                    PassageType = "Listening",
                    CreatedAt = DateTime.Parse("2024-04-05"),
                    UpdatedAt = DateTime.Parse("2024-04-08")
                },
                Part = "PART1"
            },
            new GrammarVocabularyResponseDto
            {
                Grammar = new ResponseGrammarDto
                {
                    Id = Guid.Parse("99999999-9999-9999-9999-999999999999"),
                    Name = "Participial Construction",
                    Tense = "Participle",
                    GrammarDifficulty = GrammarDifficultyEnum.medium,
                    CreatedAt = DateTime.Parse("2024-05-15"),
                    UpdatedAt = null
                },
                Vocabulary = new ResponseVocabularyDto
                {
                    Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                    KeyWord = "mellifluous",
                    VocabularyDifficulty = VocabularyDifficultyEnum.medium,
                    ToeicPart = "6",
                    PassageType = "Reading",
                    CreatedAt = DateTime.Parse("2024-05-20"),
                    UpdatedAt = DateTime.Parse("2024-05-25")
                },
                Part = "PART6"
            },
            new GrammarVocabularyResponseDto
            {
                Grammar = new ResponseGrammarDto
                {
                    Id = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
                    Name = "Ablative Absolute",
                    Tense = "Ablative",
                    GrammarDifficulty = GrammarDifficultyEnum.hard,
                    CreatedAt = DateTime.Parse("2024-06-01"),
                    UpdatedAt = DateTime.Parse("2024-06-10")
                },
                Vocabulary = new ResponseVocabularyDto
                {
                    Id = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"),
                    KeyWord = "ubiquitous",
                    VocabularyDifficulty = VocabularyDifficultyEnum.medium,
                    ToeicPart = "4",
                    PassageType = "Listening",
                    CreatedAt = DateTime.Parse("2024-06-05"),
                    UpdatedAt = null
                },
                Part = "PART4"
            },
            new GrammarVocabularyResponseDto
            {
                Grammar = new ResponseGrammarDto
                {
                    Id = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd"),
                    Name = "Emphatic Construction",
                    Tense = "Emphatic",
                    GrammarDifficulty = GrammarDifficultyEnum.easy,
                    CreatedAt = DateTime.Parse("2024-07-12"),
                    UpdatedAt = DateTime.Parse("2024-07-18")
                },
                Vocabulary = new ResponseVocabularyDto
                {
                    Id = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee"),
                    KeyWord = "perspicacious",
                    VocabularyDifficulty = VocabularyDifficultyEnum.hard,
                    ToeicPart = "2",
                    PassageType = "Listening",
                    CreatedAt = DateTime.Parse("2024-07-08"),
                    UpdatedAt = DateTime.Parse("2024-07-15")
                },
                Part = "PART2"
            }
        };

        public VocabularyGrammarControllerTest()
        {
            _mockVocabularyGrammarService = new Mock<IVocabularyGrammarService>();
            _controller = new VocabularyGrammarController(_mockVocabularyGrammarService.Object);

            SetupControllerContext();
        }

        private void SetupControllerContext()
        {
            var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Name, "testuser"),
                new Claim(ClaimTypes.Role, "Moderator"),
                new Claim("subscription", "Premium")
            }, "mock"));

            _controller.ControllerContext = new ControllerContext()
            {
                HttpContext = new DefaultHttpContext() { User = user }
            };
        }

        private static PaginationResponseDto<GrammarVocabularyResponseDto> CreateResponse(
            List<GrammarVocabularyResponseDto> data, 
            int page = 1, 
            int pageSize = 10)
        {
            return new PaginationResponseDto<GrammarVocabularyResponseDto>
            {
                Data = data,
                Pagination = new PaginationMetadata
                {
                    CurrentPage = page,
                    PageSize = pageSize,
                    TotalCount = data.Count,
                    TotalPages = (int)Math.Ceiling((double)data.Count / pageSize)
                }
            };
        }

        [Fact]
        public async Task GetUnusedPairs_WithValidPagination_ShouldReturnOkWithData()
        {
            // Arrange
            var paginationRequest = new PaginationRequestDto
            {
                Page = 1,
                PageSize = 10,
                SearchTerm = "",
                SortBy = "KeyWord",
                SortDirection = "asc"
            };

            var expectedData = _testData.OrderBy(x => x.Vocabulary!.KeyWord).Take(10).ToList();
            var expectedResponse = CreateResponse(expectedData, 1, 10);

            _mockVocabularyGrammarService
                .Setup(s => s.GetUnusedPairVocabularyGrammar(It.IsAny<PaginationRequestDto>()))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _controller.GetUnusedPairs(paginationRequest);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            okResult!.Value.Should().Be(expectedResponse);

            _mockVocabularyGrammarService.Verify(
                s => s.GetUnusedPairVocabularyGrammar(It.Is<PaginationRequestDto>(p =>
                    p.Page == paginationRequest.Page &&
                    p.PageSize == paginationRequest.PageSize)),
                Times.Once);
        }

        [Fact]
        public async Task GetUnusedPairs_WithKeyWordAscendingSort_ShouldReturnSortedResults()
        {
            // Arrange
            var paginationRequest = new PaginationRequestDto
            {
                Page = 1,
                PageSize = 10,
                SortBy = "KeyWord",
                SortDirection = "asc"
            };

            // Create sorted data from our static test data
            var sortedData = _testData.OrderBy(x => x.Vocabulary!.KeyWord).ToList();
            var expectedResponse = CreateResponse(sortedData);

            _mockVocabularyGrammarService
                .Setup(s => s.GetUnusedPairVocabularyGrammar(It.IsAny<PaginationRequestDto>()))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _controller.GetUnusedPairs(paginationRequest);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            var responseData = ((PaginationResponseDto<GrammarVocabularyResponseDto>)okResult!.Value!).Data;

            // Verify service was called with correct parameters
            _mockVocabularyGrammarService.Verify(
                s => s.GetUnusedPairVocabularyGrammar(It.Is<PaginationRequestDto>(p =>
                    p.SortBy == "KeyWord" && p.SortDirection == "asc")),
                Times.Once);

            // Verify data is sorted correctly
            var keywords = responseData.Select(x => x.Vocabulary!.KeyWord).ToList();
            keywords.Should().BeInAscendingOrder();
            keywords.Should().Equal("bibliophile", "mellifluous", "perspicacious", "quixotic", "ubiquitous", "xenophobia", "zephyr");
        }

        [Fact]
        public async Task GetUnusedPairs_WithGrammarNameSort_ShouldReturnSortedResults()
        {
            // Arrange
            var paginationRequest = new PaginationRequestDto
            {
                Page = 1,
                PageSize = 10,
                SortBy = "GrammarName",
                SortDirection = "asc"
            };

            // Create sorted data by grammar name
            var sortedData = _testData.OrderBy(x => x.Grammar!.Name).ToList();
            var expectedResponse = CreateResponse(sortedData);

            _mockVocabularyGrammarService
                .Setup(s => s.GetUnusedPairVocabularyGrammar(It.IsAny<PaginationRequestDto>()))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _controller.GetUnusedPairs(paginationRequest);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            var responseData = ((PaginationResponseDto<GrammarVocabularyResponseDto>)okResult!.Value!).Data;

            // Verify service was called with correct parameters
            _mockVocabularyGrammarService.Verify(
                s => s.GetUnusedPairVocabularyGrammar(It.Is<PaginationRequestDto>(p =>
                    p.SortBy == "GrammarName" && p.SortDirection == "asc")),
                Times.Once);

            // Verify data is sorted by grammar name
            var grammarNames = responseData.Select(x => x.Grammar!.Name).ToList();
            grammarNames.Should().BeInAscendingOrder();
        }

        [Fact]
        public async Task GetUnusedPairs_WithPartFilter_ShouldReturnFilteredResults()
        {
            // Arrange
            var paginationRequest = new PaginationRequestDto
            {
                Page = 1,
                PageSize = 10,
                Filters = new Dictionary<string, object>
                {
                    { "part", "PART5" }
                }
            };

            // Filter data for PART5 only
            var filteredData = _testData.Where(x => x.Part == "PART5").ToList();
            var expectedResponse = CreateResponse(filteredData);

            _mockVocabularyGrammarService
                .Setup(s => s.GetUnusedPairVocabularyGrammar(It.IsAny<PaginationRequestDto>()))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _controller.GetUnusedPairs(paginationRequest);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            var responseData = ((PaginationResponseDto<GrammarVocabularyResponseDto>)okResult!.Value!).Data;

            // Verify service was called with correct filter
            _mockVocabularyGrammarService.Verify(
                s => s.GetUnusedPairVocabularyGrammar(It.Is<PaginationRequestDto>(p =>
                    p.Filters != null && 
                    p.Filters.ContainsKey("part") && 
                    p.Filters["part"].ToString() == "PART5")),
                Times.Once);

            // Verify all returned data belongs to PART5
            responseData.Should().OnlyContain(item => item.Part == "PART5");
            responseData.Should().HaveCount(1);
            responseData.First().Vocabulary!.KeyWord.Should().Be("bibliophile");
        }

        [Fact]
        public async Task GetUnusedPairs_WithMultiplePartFilters_ShouldReturnFilteredResults()
        {
            // Arrange
            var paginationRequest = new PaginationRequestDto
            {
                Page = 1,
                PageSize = 15,
                Filters = new Dictionary<string, object>
                {
                    { "part", new[] { "PART1", "PART7" } }
                }
            };

            // Filter data for PART1 and PART7
            var filteredData = _testData.Where(x => x.Part == "PART1" || x.Part == "PART7").ToList();
            var expectedResponse = CreateResponse(filteredData, 1, 15);

            _mockVocabularyGrammarService
                .Setup(s => s.GetUnusedPairVocabularyGrammar(It.IsAny<PaginationRequestDto>()))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _controller.GetUnusedPairs(paginationRequest);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            var responseData = ((PaginationResponseDto<GrammarVocabularyResponseDto>)okResult!.Value!).Data;

            // Verify service was called with correct filters
            _mockVocabularyGrammarService.Verify(
                s => s.GetUnusedPairVocabularyGrammar(It.Is<PaginationRequestDto>(p =>
                    p.Filters != null && 
                    p.Filters.ContainsKey("part"))),
                Times.Once);

            // Verify returned data belongs to specified parts
            responseData.Should().OnlyContain(item => item.Part == "PART1" || item.Part == "PART7");
            responseData.Should().HaveCount(2);
        }

        [Fact]
        public async Task GetUnusedPairs_WithSearchTermThatDoesNotMatch_ShouldReturnEmptyResults()
        {
            // Arrange - using a search term that won't match our obscure vocabulary
            var paginationRequest = new PaginationRequestDto
            {
                Page = 1,
                PageSize = 10,
                SearchTerm = "commonword"
            };

            // Since our test data contains uncommon words, search should return empty
            var expectedResponse = CreateResponse(new List<GrammarVocabularyResponseDto>());

            _mockVocabularyGrammarService
                .Setup(s => s.GetUnusedPairVocabularyGrammar(It.IsAny<PaginationRequestDto>()))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _controller.GetUnusedPairs(paginationRequest);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            var responseData = ((PaginationResponseDto<GrammarVocabularyResponseDto>)okResult!.Value!).Data;

            // Verify service was called with search term
            _mockVocabularyGrammarService.Verify(
                s => s.GetUnusedPairVocabularyGrammar(It.Is<PaginationRequestDto>(p =>
                    p.SearchTerm == "commonword")),
                Times.Once);

            // Verify empty results
            responseData.Should().BeEmpty();
        }

        [Fact]
        public async Task GetUnusedPairs_WithPartFilterAndSorting_ShouldReturnFilteredAndSorted()
        {
            // Arrange
            var paginationRequest = new PaginationRequestDto
            {
                Page = 1,
                PageSize = 10,
                SortBy = "KeyWord",
                SortDirection = "asc",
                Filters = new Dictionary<string, object>
                {
                    { "part", "PART3" }
                }
            };

            // Filter and sort data for PART3
            var filteredAndSortedData = _testData
                .Where(x => x.Part == "PART3")
                .OrderBy(x => x.Vocabulary!.KeyWord)
                .ToList();
            var expectedResponse = CreateResponse(filteredAndSortedData);

            _mockVocabularyGrammarService
                .Setup(s => s.GetUnusedPairVocabularyGrammar(It.IsAny<PaginationRequestDto>()))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _controller.GetUnusedPairs(paginationRequest);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            var responseData = ((PaginationResponseDto<GrammarVocabularyResponseDto>)okResult!.Value!).Data;

            // Verify service was called with both filter and sort parameters
            _mockVocabularyGrammarService.Verify(
                s => s.GetUnusedPairVocabularyGrammar(It.Is<PaginationRequestDto>(p =>
                    p.SortBy == "KeyWord" && 
                    p.SortDirection == "asc" &&
                    p.Filters != null && 
                    p.Filters.ContainsKey("part") && 
                    p.Filters["part"].ToString() == "PART3")),
                Times.Once);

            // Verify results are filtered and sorted
            responseData.Should().OnlyContain(item => item.Part == "PART3");
            responseData.Should().HaveCount(1);
            responseData.First().Vocabulary!.KeyWord.Should().Be("quixotic");
        }

        [Fact]
        public async Task GetUnusedPairs_WithNullPagination_ShouldReturnOkWithDefaultPagination()
        {
            // Arrange
            var expectedResponse = CreateResponse(new List<GrammarVocabularyResponseDto>(), 1, 20);

            _mockVocabularyGrammarService
                .Setup(s => s.GetUnusedPairVocabularyGrammar(It.IsAny<PaginationRequestDto>()))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _controller.GetUnusedPairs(null!);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            okResult!.Value.Should().Be(expectedResponse);

            _mockVocabularyGrammarService.Verify(
                s => s.GetUnusedPairVocabularyGrammar(It.IsAny<PaginationRequestDto>()),
                Times.Once);
        }
    }
}
