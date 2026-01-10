using AutoMapper;
using BusinessLogic.DTOs.QuizDtos;
using BusinessLogic.DTOs.QuizGroupItemDtos;
using BusinessLogic.DTOs.QuizSetDtos;
using BusinessLogic.Interfaces;
using BusinessLogic.MappingProfile;
using BusinessLogic.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Repository.Entities;
using Repository.Enums;
using Repository.Interfaces;
using OfficeOpenXml;

namespace QuizUpLearn.Test.UnitTest
{
    public class PlacementQuizSetServiceTest : BaseServiceTest
    {
        private readonly Mock<IQuizSetService> _mockQuizSetService;
        private readonly Mock<IQuizRepo> _mockQuizRepo;
        private readonly Mock<IAnswerOptionRepo> _mockAnswerOptionRepo;
        private readonly Mock<IQuizQuizSetService> _mockQuizQuizSetService;
        private readonly Mock<IQuizGroupItemService> _mockQuizGroupItemService;
        private readonly IMapper _mapper;
        private readonly PlacementQuizSetService _placementQuizSetService;

        public PlacementQuizSetServiceTest()
        {
            _mockQuizSetService = new Mock<IQuizSetService>();
            _mockQuizRepo = new Mock<IQuizRepo>();
            _mockAnswerOptionRepo = new Mock<IAnswerOptionRepo>();
            _mockQuizQuizSetService = new Mock<IQuizQuizSetService>();
            _mockQuizGroupItemService = new Mock<IQuizGroupItemService>();

            var mapperConfig = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<MappingProfile>();
            }, new NullLoggerFactory());
            _mapper = mapperConfig.CreateMapper();

            _placementQuizSetService = new PlacementQuizSetService(
                _mockQuizSetService.Object,
                _mockQuizRepo.Object,
                _mockAnswerOptionRepo.Object,
                _mapper,
                _mockQuizQuizSetService.Object,
                _mockQuizGroupItemService.Object);

            ExcelPackage.License.SetNonCommercialPersonal("QuizUpLearn");
        }

        [Fact]
        public async Task ImportExcelQuizSetFile_WithValidExcelFile_ShouldReturnQuizSetResponse()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var fileName = "TOEIC_Test.xlsx";
            
            // Create a mock Excel file content in memory
            var excelContent = CreateMockExcelFileContent();
            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(f => f.FileName).Returns(fileName);
            mockFile.Setup(f => f.OpenReadStream()).Returns(new MemoryStream(excelContent));
            
            // Setup expected QuizSet creation
            var expectedQuizSet = new QuizSetResponseDto
            {
                Id = Guid.NewGuid(),
                Title = $"TOEIC Placement Test {fileName}",
                Description = "Imported from Excel file",
                QuizSetType = QuizSetTypeEnum.Placement,
                IsPublished = false,
                IsPremiumOnly = false,
                CreatedBy = userId,
                CreatedAt = DateTime.UtcNow,
                QuizGroupItems = new List<ResponseQuizGroupItemDto>()
            };

            _mockQuizSetService.Setup(s => s.CreateQuizSetAsync(It.IsAny<QuizSetRequestDto>()))
                .ReturnsAsync(expectedQuizSet);

            // Setup quiz group item creation
            var listeningGroupItem = new ResponseQuizGroupItemDto
            {
                Id = Guid.NewGuid(),
                Name = "Listening Section",
                CreatedAt = DateTime.UtcNow,
                Quizzes = new List<QuizResponseDto>()
            };

            _mockQuizGroupItemService.Setup(s => s.CreateAsync(It.IsAny<RequestQuizGroupItemDto>()))
                .ReturnsAsync(listeningGroupItem);

            // Setup batch quiz creation
            var createdQuizzes = new List<Quiz>
            {
                new Quiz
                {
                    Id = Guid.NewGuid(),
                    QuestionText = "Sample question 1",
                    TOEICPart = "PART1",
                    CorrectAnswer = "A",
                    OrderIndex = 1,
                    IsAIGenerated = false,
                    QuizGroupItemId = listeningGroupItem.Id
                },
                new Quiz
                {
                    Id = Guid.NewGuid(),
                    QuestionText = "Sample question 2",
                    TOEICPart = "PART2",
                    CorrectAnswer = "B",
                    OrderIndex = 2,
                    IsAIGenerated = false,
                    QuizGroupItemId = listeningGroupItem.Id
                }
            };

            _mockQuizRepo.Setup(r => r.CreateQuizzesBatchAsync(It.IsAny<List<Quiz>>()))
                .ReturnsAsync(createdQuizzes);

            // Setup answer option batch creation
            _mockAnswerOptionRepo.Setup(r => r.CreateBatchAsync(It.IsAny<List<AnswerOption>>()))
                .ReturnsAsync(new List<AnswerOption>());

            // Setup quiz-quizset association
            _mockQuizQuizSetService.Setup(s => s.AddQuizzesToQuizSetAsync(It.IsAny<List<Guid>>(), It.IsAny<Guid>())).ReturnsAsync(true);

            // Act
            var result = await _placementQuizSetService.ImportExcelQuizSetFile(mockFile.Object, userId);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().Be(expectedQuizSet.Id);
            result.Title.Should().Be($"TOEIC Placement Test {fileName}");
            result.Description.Should().Be("Imported from Excel file");
            result.QuizSetType.Should().Be(QuizSetTypeEnum.Placement);
            result.IsPublished.Should().BeFalse();
            result.IsPremiumOnly.Should().BeFalse();
            result.CreatedBy.Should().Be(userId);

            // Verify all mocks were called
            _mockQuizSetService.Verify(s => s.CreateQuizSetAsync(It.Is<QuizSetRequestDto>(dto => 
                dto.Title == $"TOEIC Placement Test {fileName}" &&
                dto.Description == "Imported from Excel file" &&
                dto.QuizSetType == QuizSetTypeEnum.Placement &&
                dto.CreatedBy == userId &&
                dto.IsPublished == false &&
                dto.IsPremiumOnly == false)), Times.Once);

            _mockQuizGroupItemService.Verify(s => s.CreateAsync(It.Is<RequestQuizGroupItemDto>(dto => 
                dto.Name == "Listening Section")), Times.Once);

            _mockQuizRepo.Verify(r => r.CreateQuizzesBatchAsync(It.IsAny<List<Quiz>>()), Times.Once);
            _mockAnswerOptionRepo.Verify(r => r.CreateBatchAsync(It.IsAny<List<AnswerOption>>()), Times.Once);
            _mockQuizQuizSetService.Verify(s => s.AddQuizzesToQuizSetAsync(It.IsAny<List<Guid>>(), expectedQuizSet.Id), Times.Once);
        }

        [Fact]
        public async Task ImportExcelQuizSetFile_WithListeningQuestions_ShouldAssignToListeningGroup()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var fileName = "listening_test.xlsx";
            
            var excelContent = CreateMockExcelFileContentWithListeningQuestions();
            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(f => f.FileName).Returns(fileName);
            mockFile.Setup(f => f.OpenReadStream()).Returns(new MemoryStream(excelContent));
            
            var expectedQuizSet = new QuizSetResponseDto
            {
                Id = Guid.NewGuid(),
                Title = $"TOEIC Placement Test {fileName}",
                Description = "Imported from Excel file",
                QuizSetType = QuizSetTypeEnum.Placement,
                CreatedBy = userId,
                QuizGroupItems = new List<ResponseQuizGroupItemDto>()
            };

            _mockQuizSetService.Setup(s => s.CreateQuizSetAsync(It.IsAny<QuizSetRequestDto>()))
                .ReturnsAsync(expectedQuizSet);

            var listeningGroupItem = new ResponseQuizGroupItemDto
            {
                Id = Guid.NewGuid(),
                Name = "Listening Section",
                CreatedAt = DateTime.UtcNow,
                Quizzes = new List<QuizResponseDto>()
            };

            _mockQuizGroupItemService.Setup(s => s.CreateAsync(It.IsAny<RequestQuizGroupItemDto>()))
                .ReturnsAsync(listeningGroupItem);

            var createdQuizzes = new List<Quiz>
            {
                new Quiz
                {
                    Id = Guid.NewGuid(),
                    QuestionText = "Listening question 1",
                    TOEICPart = "PART1",
                    CorrectAnswer = "A",
                    OrderIndex = 1,
                    IsAIGenerated = false,
                    QuizGroupItemId = listeningGroupItem.Id
                }
            };

            _mockQuizRepo.Setup(r => r.CreateQuizzesBatchAsync(It.Is<List<Quiz>>(quizzes => 
                quizzes.All(q => q.QuizGroupItemId == listeningGroupItem.Id))))
                .ReturnsAsync(createdQuizzes);

            _mockAnswerOptionRepo.Setup(r => r.CreateBatchAsync(It.IsAny<List<AnswerOption>>()))
                .ReturnsAsync(new List<AnswerOption>());

            _mockQuizQuizSetService.Setup(s => s.AddQuizzesToQuizSetAsync(It.IsAny<List<Guid>>(), It.IsAny<Guid>())).ReturnsAsync(true);

            // Act
            var result = await _placementQuizSetService.ImportExcelQuizSetFile(mockFile.Object, userId);

            // Assert
            result.Should().NotBeNull();
            
            // Verify that listening questions (Part 1-4) are assigned to listening group
            _mockQuizRepo.Verify(r => r.CreateQuizzesBatchAsync(It.Is<List<Quiz>>(quizzes => 
                quizzes.All(q => q.QuizGroupItemId == listeningGroupItem.Id))), Times.Once);
        }

        [Fact]
        public async Task ImportExcelQuizSetFile_WithReadingQuestions_ShouldNotAssignToListeningGroup()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var fileName = "reading_test.xlsx";
            
            var excelContent = CreateMockExcelFileContentWithReadingQuestions();
            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(f => f.FileName).Returns(fileName);
            mockFile.Setup(f => f.OpenReadStream()).Returns(new MemoryStream(excelContent));
            
            var expectedQuizSet = new QuizSetResponseDto
            {
                Id = Guid.NewGuid(),
                Title = $"TOEIC Placement Test {fileName}",
                Description = "Imported from Excel file",
                QuizSetType = QuizSetTypeEnum.Placement,
                CreatedBy = userId,
                QuizGroupItems = new List<ResponseQuizGroupItemDto>()
            };

            _mockQuizSetService.Setup(s => s.CreateQuizSetAsync(It.IsAny<QuizSetRequestDto>()))
                .ReturnsAsync(expectedQuizSet);

            var listeningGroupItem = new ResponseQuizGroupItemDto
            {
                Id = Guid.NewGuid(),
                Name = "Listening Section",
                CreatedAt = DateTime.UtcNow,
                Quizzes = new List<QuizResponseDto>()
            };

            _mockQuizGroupItemService.Setup(s => s.CreateAsync(It.IsAny<RequestQuizGroupItemDto>()))
                .ReturnsAsync(listeningGroupItem);

            var createdQuizzes = new List<Quiz>
            {
                new Quiz
                {
                    Id = Guid.NewGuid(),
                    QuestionText = "Reading question 1",
                    TOEICPart = "PART5",
                    CorrectAnswer = "C",
                    OrderIndex = 1,
                    IsAIGenerated = false,
                    QuizGroupItemId = null // Reading questions should not be assigned to listening group
                }
            };

            _mockQuizRepo.Setup(r => r.CreateQuizzesBatchAsync(It.Is<List<Quiz>>(quizzes => 
                quizzes.All(q => q.QuizGroupItemId == null))))
                .ReturnsAsync(createdQuizzes);

            _mockAnswerOptionRepo.Setup(r => r.CreateBatchAsync(It.IsAny<List<AnswerOption>>()))
                .ReturnsAsync(new List<AnswerOption>());

            _mockQuizQuizSetService.Setup(s => s.AddQuizzesToQuizSetAsync(It.IsAny<List<Guid>>(), It.IsAny<Guid>())).ReturnsAsync(true);

            // Act
            var result = await _placementQuizSetService.ImportExcelQuizSetFile(mockFile.Object, userId);

            // Assert
            result.Should().NotBeNull();
            
            // Verify that reading questions (Part 5+) are not assigned to listening group
            _mockQuizRepo.Verify(r => r.CreateQuizzesBatchAsync(It.Is<List<Quiz>>(quizzes => 
                quizzes.All(q => q.QuizGroupItemId == null))), Times.Once);
        }

        [Fact]
        public async Task ImportExcelQuizSetFile_WithMultipleQuestions_ShouldCreateCorrectAnswerOptions()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var fileName = "multiple_questions.xlsx";
            
            var excelContent = CreateMockExcelFileContentWithMultipleQuestions();
            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(f => f.FileName).Returns(fileName);
            mockFile.Setup(f => f.OpenReadStream()).Returns(new MemoryStream(excelContent));
            
            var expectedQuizSet = new QuizSetResponseDto
            {
                Id = Guid.NewGuid(),
                Title = $"TOEIC Placement Test {fileName}",
                Description = "Imported from Excel file",
                QuizSetType = QuizSetTypeEnum.Placement,
                CreatedBy = userId,
                QuizGroupItems = new List<ResponseQuizGroupItemDto>()
            };

            _mockQuizSetService.Setup(s => s.CreateQuizSetAsync(It.IsAny<QuizSetRequestDto>()))
                .ReturnsAsync(expectedQuizSet);

            var listeningGroupItem = new ResponseQuizGroupItemDto
            {
                Id = Guid.NewGuid(),
                Name = "Listening Section",
                CreatedAt = DateTime.UtcNow,
                Quizzes = new List<QuizResponseDto>()
            };

            _mockQuizGroupItemService.Setup(s => s.CreateAsync(It.IsAny<RequestQuizGroupItemDto>()))
                .ReturnsAsync(listeningGroupItem);

            var createdQuizzes = new List<Quiz>
            {
                new Quiz { Id = Guid.NewGuid(), QuestionText = "Question 1", CorrectAnswer = "A", TOEICPart = "Part1" },
                new Quiz { Id = Guid.NewGuid(), QuestionText = "Question 2", CorrectAnswer = "B", TOEICPart = "Part1" }
            };

            _mockQuizRepo.Setup(r => r.CreateQuizzesBatchAsync(It.IsAny<List<Quiz>>()))
                .ReturnsAsync(createdQuizzes);

            _mockAnswerOptionRepo.Setup(r => r.CreateBatchAsync(It.Is<List<AnswerOption>>(options =>
                options.Count == 8 &&
                options.Count(o => o.IsCorrect) == 2)))
                .ReturnsAsync(new List<AnswerOption>());

            _mockQuizQuizSetService.Setup(s => s.AddQuizzesToQuizSetAsync(It.IsAny<List<Guid>>(), It.IsAny<Guid>())).ReturnsAsync(true);

            // Act
            var result = await _placementQuizSetService.ImportExcelQuizSetFile(mockFile.Object, userId);

            // Assert
            result.Should().NotBeNull();
            
            _mockAnswerOptionRepo.Verify(r => r.CreateBatchAsync(It.Is<List<AnswerOption>>(options =>
                options.Count == 8 &&
                options.Count(o => o.IsCorrect) == 2 &&
                options.All(o => !string.IsNullOrEmpty(o.OptionLabel)) &&
                options.All(o => !string.IsNullOrEmpty(o.OptionText)))), Times.Once);
        }

        private byte[] CreateMockExcelFileContent()
        {
            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("Questions");
            
            // Add sample data for general questions
            worksheet.Cells[2, 1].Value = 1; // Part
            worksheet.Cells[2, 2].Value = 1; // GlobalIndex  
            worksheet.Cells[2, 3].Value = "Sample question 1"; // Prompt
            worksheet.Cells[2, 4].Value = "Option A"; // Choice A
            worksheet.Cells[2, 5].Value = "Option B"; // Choice B
            worksheet.Cells[2, 6].Value = "Option C"; // Choice C
            worksheet.Cells[2, 7].Value = "Option D"; // Choice D
            worksheet.Cells[2, 8].Value = "A"; // CorrectAnswer
            
            worksheet.Cells[3, 1].Value = 2; // Part
            worksheet.Cells[3, 2].Value = 2; // GlobalIndex
            worksheet.Cells[3, 3].Value = "Sample question 2"; // Prompt
            worksheet.Cells[3, 4].Value = "Option A2"; // Choice A
            worksheet.Cells[3, 5].Value = "Option B2"; // Choice B
            worksheet.Cells[3, 6].Value = "Option C2"; // Choice C
            worksheet.Cells[3, 7].Value = "Option D2"; // Choice D
            worksheet.Cells[3, 8].Value = "B"; // CorrectAnswer
            
            return package.GetAsByteArray();
        }

        private byte[] CreateMockExcelFileContentWithListeningQuestions()
        {
            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("Questions");
            
            // Add listening questions (Part 1-4)
            worksheet.Cells[2, 1].Value = 1; // Part 1 (Listening)
            worksheet.Cells[2, 2].Value = 1; // GlobalIndex
            worksheet.Cells[2, 3].Value = "Listening question 1"; // Prompt
            worksheet.Cells[2, 4].Value = "Option A"; // Choice A
            worksheet.Cells[2, 5].Value = "Option B"; // Choice B
            worksheet.Cells[2, 6].Value = "Option C"; // Choice C
            worksheet.Cells[2, 7].Value = "Option D"; // Choice D
            worksheet.Cells[2, 8].Value = "A"; // CorrectAnswer
            
            return package.GetAsByteArray();
        }

        private byte[] CreateMockExcelFileContentWithReadingQuestions()
        {
            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("Questions");
            
            // Add reading questions (Part 5+)
            worksheet.Cells[2, 1].Value = 5; // Part 5 (Reading)
            worksheet.Cells[2, 2].Value = 1; // GlobalIndex
            worksheet.Cells[2, 3].Value = "Reading question 1"; // Prompt
            worksheet.Cells[2, 4].Value = "Option A"; // Choice A
            worksheet.Cells[2, 5].Value = "Option B"; // Choice B
            worksheet.Cells[2, 6].Value = "Option C"; // Choice C
            worksheet.Cells[2, 7].Value = "Option D"; // Choice D
            worksheet.Cells[2, 8].Value = "C"; // CorrectAnswer
            
            return package.GetAsByteArray();
        }

        private byte[] CreateMockExcelFileContentWithMultipleQuestions()
        {
            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("Questions");
            
            // Add multiple questions
            worksheet.Cells[2, 1].Value = 1; // Part
            worksheet.Cells[2, 2].Value = 1; // GlobalIndex
            worksheet.Cells[2, 3].Value = "Question 1"; // Prompt
            worksheet.Cells[2, 4].Value = "Option A1"; // Choice A
            worksheet.Cells[2, 5].Value = "Option B1"; // Choice B
            worksheet.Cells[2, 6].Value = "Option C1"; // Choice C
            worksheet.Cells[2, 7].Value = "Option D1"; // Choice D
            worksheet.Cells[2, 8].Value = "A"; // CorrectAnswer
            
            worksheet.Cells[3, 1].Value = 1; // Part
            worksheet.Cells[3, 2].Value = 2; // GlobalIndex
            worksheet.Cells[3, 3].Value = "Question 2"; // Prompt
            worksheet.Cells[3, 4].Value = "Option A2"; // Choice A
            worksheet.Cells[3, 5].Value = "Option B2"; // Choice B
            worksheet.Cells[3, 6].Value = "Option C2"; // Choice C
            worksheet.Cells[3, 7].Value = "Option D2"; // Choice D
            worksheet.Cells[3, 8].Value = "B"; // CorrectAnswer
            
            return package.GetAsByteArray();
        }
    }
}