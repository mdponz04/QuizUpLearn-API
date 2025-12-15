using AutoMapper;
using BusinessLogic.DTOs;
using BusinessLogic.DTOs.PlacementQuizSetDtos;
using BusinessLogic.DTOs.QuizDtos;
using BusinessLogic.DTOs.QuizGroupItemDtos;
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
        private readonly IQuizQuizSetService _quizQuizSetService;
        private readonly IQuizGroupItemService _quizGroupItemService;

        public PlacementQuizSetService(IQuizSetService quizSetService,
            IQuizRepo quizRepo,
            IAnswerOptionRepo answerOptionRepo,
            IMapper mapper,
            IQuizQuizSetService quizQuizSetService,
            IQuizGroupItemService quizGroupItemService)
        {
            _quizSetService = quizSetService;
            _quizRepo = quizRepo;
            _answerOptionRepo = answerOptionRepo;
            _mapper = mapper;
            _quizQuizSetService = quizQuizSetService;
            _quizGroupItemService = quizGroupItemService;
        }


        public async Task<QuizSetResponseDto> ImportExcelQuizSetFile(IFormFile file, Guid userId)
        {
            await using var stream = file.OpenReadStream();
            var quizzesData = ExtractQuestions(stream);

            var quizSet = await _quizSetService.CreateQuizSetAsync(new QuizSetRequestDto 
            {
                Title = $"TOEIC Placement Test {file.FileName}",
                Description = "Imported from Excel file",
                IsPublished = false,
                IsAIGenerated = false,
                IsPremiumOnly = false,
                QuizSetType = Repository.Enums.QuizSetTypeEnum.Placement,
                CreatedBy = userId
            });
            
            var quizzesToInsert = new List<Quiz>();
            
            var quizGroupItemListening = await _quizGroupItemService.CreateAsync(new RequestQuizGroupItemDto
            {
                Name = "Listening Section",
            });

            if (quizSet.QuizGroupItems == null)
                throw new Exception("Quiz group item listening is null after creating QuizSet.");

            foreach (var quizData in quizzesData)
            {
                var quiz = _mapper.Map<Quiz>(new QuizRequestDto
                {
                    QuestionText = quizData.Prompt,
                    OrderIndex = quizData.GlobalIndex,
                    TOEICPart = $"PART{quizData.Part}",
                    CorrectAnswer = quizData.CorrectAnswer,
                    QuizGroupItemId = quizGroupItemListening.Id,
                });
                quizzesToInsert.Add(quiz);
            }

            var createdQuizzes = await _quizRepo.CreateQuizzesBatchAsync(quizzesToInsert);
            var listQuizIds = createdQuizzes.Select(q => q.Id).ToList();

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

            await _answerOptionRepo.CreateBatchAsync(answerOptionsToInsert);
            await _quizQuizSetService.AddQuizzesToQuizSetAsync(listQuizIds, quizSet.Id);

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
