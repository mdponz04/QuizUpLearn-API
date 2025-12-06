using AutoMapper;
using BusinessLogic.DTOs;
using BusinessLogic.DTOs.PlacementQuizSetDtos;
using BusinessLogic.DTOs.QuizDtos;
using BusinessLogic.DTOs.QuizSetDtos;
using BusinessLogic.Interfaces;
using Microsoft.AspNetCore.Http;
using OfficeOpenXml;
using Repository.Entities;
using Repository.Interfaces;


namespace BusinessLogic.Services
{
    public class PlacementQuizSetService : IPlacementQuizSetService
    {
        private readonly IQuizSetService _quizSetService;
        private readonly IQuizRepo _quizRepo;
        private readonly IAnswerOptionRepo _answerOptionRepo;
        private readonly IMapper _mapper;

        public PlacementQuizSetService(IQuizSetService quizSetService, 
            IQuizRepo quizRepo,
            IAnswerOptionRepo answerOptionRepo,
            IMapper mapper)
        {
            _quizSetService = quizSetService;
            _quizRepo = quizRepo;
            _answerOptionRepo = answerOptionRepo;
            _mapper = mapper;
        }


        public async Task<QuizSetResponseDto> ImportExcelQuizSetFile(IFormFile file, Guid userId)
        {
            await using var stream = file.OpenReadStream();
            var quizzesData = ExtractQuestions(stream);

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
            
            // Tạo tất cả quizzes trước (batch insert)
            var quizzesToInsert = new List<Quiz>();
            foreach(var quizData in quizzesData)
            {
                var quiz = _mapper.Map<Quiz>(new QuizRequestDto
                {
                    QuestionText = quizData.Prompt,
                    OrderIndex = quizData.GlobalIndex,
                    TOEICPart = $"PART{quizData.Part}",
                    QuizSetId = quizSet.Id,
                    CorrectAnswer = quizData.CorrectAnswer
                });
                quizzesToInsert.Add(quiz);
            }

            // Batch insert tất cả quizzes cùng lúc
            var createdQuizzes = await _quizRepo.CreateQuizzesBatchAsync(quizzesToInsert);

            // Tạo tất cả answer options (batch insert)
            var answerOptionsToInsert = new List<AnswerOption>();
            var quizList = createdQuizzes.ToList();
            for (int i = 0; i < quizList.Count; i++)
            {
                var quiz = quizList[i];
                var quizData = quizzesData[i];
                
                int orderIndex = 0;
                foreach(var choice in quizData.Choices)
                {
                    var answerOption = _mapper.Map<AnswerOption>(new RequestAnswerOptionDto
                    {
                        QuizId = quiz.Id,
                        OptionText = choice.Text,
                        IsCorrect = choice.Label == quizData.CorrectAnswer,
                        OptionLabel = choice.Label,
                        OrderIndex = orderIndex++
                    });
                    answerOptionsToInsert.Add(answerOption);
                }
            }

            // Batch insert tất cả answer options cùng lúc
            await _answerOptionRepo.CreateBatchAsync(answerOptionsToInsert);

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
