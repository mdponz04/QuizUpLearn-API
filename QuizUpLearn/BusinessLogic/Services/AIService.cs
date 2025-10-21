using BusinessLogic.DTOs;
using BusinessLogic.DTOs.AiDtos;
using BusinessLogic.DTOs.QuizDtos;
using BusinessLogic.DTOs.QuizSetDtos;
using BusinessLogic.Interfaces;
using Microsoft.Extensions.Configuration;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace BusinessLogic.Services
{
    public class AIService : IAIService
    {
        private readonly HttpClient _httpClient;
        private readonly IQuizSetService _quizSetService;
        private readonly IQuizService _quizService;
        private readonly IAnswerOptionService _answerOptionService;
        private readonly string _geminiApiKey;
        private readonly string _openRouterApiKey;

        public AIService(HttpClient httpClient, IConfiguration configuration, IQuizSetService quizSetService, IQuizService quizService, IAnswerOptionService answerOptionService)
        {
            _httpClient = httpClient;
            _geminiApiKey = configuration["Gemini:ApiKey"] ?? throw new ArgumentNullException("Gemini API key is not configured.");
            _openRouterApiKey = configuration["OpenRouter:ApiKey"] ?? throw new ArgumentNullException("open router API key is not configured.");
            _quizSetService = quizSetService;
            _quizService = quizService;
            _answerOptionService = answerOptionService;
        }

        public Task AnalyzeUserProgress()
        {
            throw new NotImplementedException();
        }

        public async Task<string> GeminiGenerateContentAsync(string prompt)
        {
            var url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash-lite:generateContent";

            var body = new
            {
                contents = new[]
                {
                    new
                    {
                        role = "USER",
                        parts = new[]
                        {
                            new { text = prompt }
                        }
                    }
                },
                generationConfig = new
                {
                    maxOutputTokens = 512,
                    temperature = 0.7,
                    responseMimeType = "application/json"
                }
            };

            var json = JsonSerializer.Serialize(body);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Add("x-goog-api-key", _geminiApiKey);
            request.Content = content;

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
                return $"Error: {response.StatusCode}";

            var responseString = await response.Content.ReadAsStringAsync();

            using var doc = JsonDocument.Parse(responseString);
            try
            {
                return doc.RootElement
                          .GetProperty("candidates")[0]
                          .GetProperty("content")
                          .GetProperty("parts")[0]
                          .GetProperty("text")
                          .GetString() ?? "No answer returned.";
            }
            catch
            {
                return "Failed to parse response.";
            }
        }

        private async Task<string> GenerateContentAsync(string prompt)
        {
            
            var url = "https://openrouter.ai/api/v1/chat/completions";

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_openRouterApiKey}");   

            var body = new
            {
                model = "meta-llama/llama-4-maverick:free",
                messages = new[]
                {
                    new 
                    { 
                        role = "user",
                        content = prompt
                    }
                }
            };

            var json = JsonSerializer.Serialize(body);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(url, content);
            var result = await response.Content.ReadAsStringAsync();

            using var doc = JsonDocument.Parse(result);
            if (!doc.RootElement.TryGetProperty("choices", out var choices))
                return json; // return raw response if structure unexpected

            var returnContent = doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();

            if (!string.IsNullOrWhiteSpace(returnContent))
            {
                returnContent = returnContent
                    .Replace("<｜begin▁of▁sentence｜>", "")
                    .Replace("<｜end▁of▁sentence｜>", "")
                    .Trim();

                var match = Regex.Match(returnContent, "```json(.*?)```", RegexOptions.Singleline);
                returnContent = match.Success ? match.Groups[1].Value.Trim() : returnContent.Trim();
            }
            else
            {
                returnContent = null;
            }

            return returnContent;
        }

        public async Task<QuizSetResponseDto> GeneratePracticeQuizSetAsync(AiGenerateQuizSetRequestDto inputData)
        {
            var prompt = $@"
Generate a TOEIC practice quiz titled: '{inputData.Topic}'.
Description: Focus on {inputData.SkillType} skills (TOEIC Part {inputData.ToeicPart}), 
suitable for learners with TOEIC scores around {inputData.Difficulty}.

Generate ONE question that matches this theme.

Return in this structure:
{{
  ""QuestionText"": ""..."",
  ""AnswerOptions"": [
    {{ ""OptionLabel"": ""A"", ""OptionText"": ""..."", ""IsCorrect"": true/false }},
    {{ ""OptionLabel"": ""B"", ""OptionText"": ""..."", ""IsCorrect"": true/false }},
    {{ ""OptionLabel"": ""C"", ""OptionText"": ""..."", ""IsCorrect"": true/false }},
    {{ ""OptionLabel"": ""D"", ""OptionText"": ""..."", ""IsCorrect"": true/false }}
  ]
}}";
            var createdQuizSet = await _quizSetService.CreateQuizSetAsync(new QuizSetRequestDto
            {
                Title = inputData.Topic,
                Description = $"AI-generated TOEIC practice quiz on {inputData.Topic}, focusing on {inputData.SkillType} skills.",
                QuizType = "Practice",
                SkillType = inputData.SkillType,
                DifficultyLevel = inputData.Difficulty,
                CreatedBy = inputData.CreatorId
            });


            for (int i = 0; i < inputData.QuestionQuantity; i++)
            {
                var response = await GeminiGenerateContentAsync(prompt);

                var quiz = JsonSerializer.Deserialize<AiGenerateQuizResponseDto>(response);
                if (quiz == null 
                    || string.IsNullOrEmpty(quiz.QuestionText) 
                    || quiz.AnswerOptions.Count == 0)
                    throw new Exception("Failed to generate valid quiz data from AI.");

                var createdQuiz = await _quizService.CreateQuizAsync(new QuizRequestDto
                {
                    QuizSetId = createdQuizSet.Id,
                    QuestionText = quiz.QuestionText,
                    TOEICPart = inputData.ToeicPart,
                });

                foreach(var item in quiz.AnswerOptions)
                {
                    await _answerOptionService.CreateAsync(new RequestAnswerOptionDto
                    {
                        OptionLabel = item.OptionLabel,
                        OptionText = item.OptionText,
                        IsCorrect = item.IsCorrect,
                        QuizId = createdQuiz.Id
                    });
                }
            }

            return createdQuizSet;
        }

        public async Task<(bool, string)> ValidateQuizSetAsync(Guid quizSetId)
        {
            var quizSet = await _quizSetService.GetQuizSetByIdAsync(quizSetId);
            if (quizSet == null) return (false, "Quiz set not found.");

            var quizzes = await _quizService.GetQuizzesByQuizSetIdAsync(quizSetId);
            if (quizzes == null)
                return (false, "No quizzes found in this set.");

            bool allValid = true;
            var feedbackBuilder = new StringBuilder();

            foreach (var quiz in quizzes)
            {
                var options = await _answerOptionService.GetByQuizIdAsync(quiz.Id);

                var validationPrompt = $@"
You are an expert TOEIC test validator.
Review this quiz for correctness and clarity.

### Quiz Set Context:
TOEIC practice quiz titled: '{quizSet.Title}'.
Description: Focus on {quizSet.SkillType} skills (TOEIC Part {quizSet.TOEICPart}), 
suitable for learners with TOEIC scores around {quizSet.DifficultyLevel}.

### Quiz to Validate
Question: {quiz.QuestionText}
Options:
{string.Join("\n", options.Select(o => $"{o.OptionLabel}. {o.OptionText} (Correct: {o.IsCorrect})"))}

Check the criteria and explain shortly at Feedback field:
1. The question is grammatically correct and meaningful.
2. There is ONE or more correct answer.
3. The correct answer makes sense in context.

Return only these 2 fields as JSON structure:
{{ 
    ""IsValid"": true/false,
    ""Feedback"": "".....""
}}
";

                var response = await GenerateContentAsync(validationPrompt);
                
                
                var validation = JsonSerializer.Deserialize<AIValidationResponseDto>(response ?? string.Empty);

                if (validation == null)
                {
                    feedbackBuilder.AppendLine($"Quiz {quiz.Id}: Validation failed (null response).");
                    allValid = false;
                    continue;
                }

                feedbackBuilder.AppendLine($"Quiz {quiz.QuestionText}: {validation.Feedback}");

                if (!validation.IsValid)
                    allValid = false;
            }

            return (allValid, feedbackBuilder.ToString());
        }

    }
}
