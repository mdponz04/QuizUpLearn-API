using BusinessLogic.DTOs;
using BusinessLogic.DTOs.PlacementQuizSetDtos;
using BusinessLogic.DTOs.QuizDtos;
using BusinessLogic.DTOs.QuizSetDtos;
using BusinessLogic.Interfaces;
using Microsoft.AspNetCore.Http;
using OfficeOpenXml;


namespace BusinessLogic.Services
{
    public class PlacementQuizSetService : IPlacementQuizSetService
    {
        private readonly IQuizSetService _quizSetService;
        private readonly IQuizService _quizService;
        private readonly IAnswerOptionService _answerOptionService;

        public PlacementQuizSetService(IQuizSetService quizSetService, 
            IQuizService quizService, 
            IAnswerOptionService answerOptionService)
        {
            _quizSetService = quizSetService;
            _quizService = quizService;
            _answerOptionService = answerOptionService;
        }


        public async Task<QuizSetResponseDto> ImportExcelQuizSetFile(IFormFile file, Guid userId)
        {
            await using var stream = file.OpenReadStream();
            var quizzes = ExtractQuestions(stream);

            var quizSet = await _quizSetService.CreateQuizSetAsync(new QuizSetRequestDto 
            {
                Title = $"TOEIC Placement Test {file.FileName}",
                Description = "Imported from Excel file",
                IsPublished = true,
                IsAIGenerated = false,
                IsPremiumOnly = false,
                QuizSetType = Repository.Enums.QuizSetTypeEnum.Placement,
                CreatedBy = userId
            });
            
            foreach(var quiz in quizzes)
            {
                var newQuiz = await _quizService.CreateQuizAsync(new QuizRequestDto
                {
                    QuestionText = quiz.Prompt,
                    OrderIndex = quiz.GlobalIndex,
                    TOEICPart = $"PART{quiz.Part}",
                    QuizSetId = quizSet.Id,
                    CorrectAnswer = quiz.CorrectAnswer
                });

                foreach(var choice in quiz.Choices)
                {
                    await _answerOptionService.CreateAsync(new RequestAnswerOptionDto
                    {
                        QuizId = newQuiz.Id,
                        OptionText = choice.Text,
                        IsCorrect = choice.Label == quiz.CorrectAnswer,
                        OptionLabel = choice.Label
                    });
                }
            }

            return quizSet;
        }

        private List<PlacementQuizSetImportDto> ExtractQuestions(Stream excelStream)
        {
            ExcelPackage.License.SetNonCommercialPersonal("QuizUpLearn");

            using var package = new ExcelPackage(excelStream);
            var worksheet = package.Workbook.Worksheets["Questions"];
            if(worksheet == null)
            {
                throw new Exception("Worksheet 'Questions' not found in the Excel file.");
            }

            var result = new List<PlacementQuizSetImportDto>();

            int startRow = 2;
            while (true)
            {
                var partCell = worksheet.Cells[startRow, 1];
                if (partCell == null || partCell.Value == null) break;

                var dto = new PlacementQuizSetImportDto
                {
                    Part = Convert.ToInt32(worksheet.Cells[startRow, 1].Value),
                    GlobalIndex = Convert.ToInt32(worksheet.Cells[startRow, 2].Value),
                    Prompt = worksheet.Cells[startRow, 3].GetValue<string>(),
                    Choices = new List<Choice>
                    {
                        new Choice { Label = "A", Text = worksheet.Cells[startRow, 4].Text },
                        new Choice { Label = "B", Text = worksheet.Cells[startRow, 5].Text },
                        new Choice { Label = "C", Text = worksheet.Cells[startRow, 6].Text },
                        new Choice { Label = "D", Text = worksheet.Cells[startRow, 7].Text }
                    },
                    CorrectAnswer = worksheet.Cells[startRow, 8].GetValue<string>()
                };

                result.Add(dto);
                startRow++;
            }

            return result;
        }
    }
}
