using BusinessLogic.DTOs;
using BusinessLogic.DTOs.AiDtos;
using BusinessLogic.DTOs.QuizDtos;
using BusinessLogic.DTOs.QuizSetDtos;
using BusinessLogic.Helpers;
using BusinessLogic.Interfaces;
using Microsoft.Extensions.Configuration;
using System.Net.Http.Headers;
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
        private readonly IUploadService _uploadService;
        //API keys
        private readonly string _geminiApiKey;
        private readonly string _openRouterApiKey;
        private readonly string _elevenLabsApiKey;
        private readonly string _nebiusApiKey;
        //Voice IDs
        private readonly string _maleVoiceId;
        private readonly string _femaleVoiceId;
        private readonly string _narratorVoiceId;
        public AIService(HttpClient httpClient, IConfiguration configuration, IQuizSetService quizSetService, IQuizService quizService, IAnswerOptionService answerOptionService, IUploadService uploadService)
        {
            _httpClient = httpClient;
            _quizSetService = quizSetService;
            _quizService = quizService;
            _answerOptionService = answerOptionService;
            _uploadService = uploadService;
            //API keys
            _geminiApiKey = configuration["Gemini:ApiKey"] ?? throw new ArgumentNullException("Gemini API key is not configured.");
            _openRouterApiKey = configuration["OpenRouter:ApiKey"] ?? throw new ArgumentNullException("Open router API key is not configured.");
            _elevenLabsApiKey = configuration["ElevenLabs:ApiKey"] ?? throw new ArgumentNullException("Eleven Labs API key is not configured.");
            _nebiusApiKey = configuration["Nebius:ApiKey"] ?? throw new ArgumentNullException("Nebius API key is not configured.");
            //Voice ids
            _maleVoiceId = configuration["ElevenLabs:Voices:Male"] ?? throw new ArgumentNullException("Male voice id is not configured");
            _femaleVoiceId = configuration["ElevenLabs:Voices:Female"] ?? throw new ArgumentNullException("Female voice id is not configured"); ;
            _narratorVoiceId = configuration["ElevenLabs:Voices:Narrator"] ?? throw new ArgumentNullException("Narrator voice id is not configured");
            
        }

        public Task AnalyzeUserProgress()
        {
            throw new NotImplementedException();
        }

        private async Task<string> GeminiGenerateContentAsync(string prompt)
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

        private async Task<byte[]> GenerateImageAsync(string prompt)
        {
            var url = $"https://api.studio.nebius.com/v1/images/generations";

            var payload = new
            {
                model = "black-forest-labs/flux-schnell",
                prompt = prompt,
                response_format = "b64_json",
                response_extension = "png",
                width = 1024,
                height = 768,
                num_inference_steps = 8,
                negative_prompt = "color",
                seed = -1,
                loras = (object?)null
            };

            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _nebiusApiKey);
            request.Content = content;

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var base64 = doc.RootElement.GetProperty("data")[0].GetProperty("b64_json").GetString();

            return Convert.FromBase64String(base64!);
        }

        private string GetVoiceId(VoiceRoles role)
        {
            switch (role)
            {
                case VoiceRoles.Male:
                    return _maleVoiceId;
                case VoiceRoles.Female:
                    return _femaleVoiceId;
                case VoiceRoles.Narrator:
                    return _narratorVoiceId;
                default:
                    throw new ArgumentOutOfRangeException(nameof(role), role, "Invalid voice role");
            }
        }

        private async Task<byte[]> GenerateAudioAsync(string text, VoiceRoles role)
        {
            var voiceId = GetVoiceId(role);
            var url = $"https://api.elevenlabs.io/v1/text-to-speech/{voiceId}";

            var requestBody = new
            {
                text,
                model_id = "eleven_multilingual_v2",
                voice_settings = new { stability = 0.5, similarity_boost = 0.8 }
            };

            var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Add("xi-api-key", _elevenLabsApiKey);
            request.Content = new StringContent(
                System.Text.Json.JsonSerializer.Serialize(requestBody),
                System.Text.Encoding.UTF8,
                "application/json"
            );

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsByteArrayAsync();
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
Description: Focus on TOEIC Part {quizSet.TOEICPart}, 
suitable for learners with TOEIC scores around {quizSet.DifficultyLevel}.

### Quiz to Validate
Question: {quiz.QuestionText}
Options:
{string.Join("\n", options.Select(o => $"{o.OptionLabel}. {o.OptionText} (Correct: {o.IsCorrect})"))}

Check the criteria and explain shortly at Feedback field:
1. The question is grammatically correct and meaningful.
2. There is ONE or more correct answer.
3. The correct answer makes sense in context.
4. Not duplicating or very similar options.
5. If everything is fine but can be improved just give suggestion in Feedback but the is valid is depend on 4 above criteria.

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
        //Done
        public async Task<QuizSetResponseDto> GeneratePracticeQuizSetPart1Async(AiGenerateQuizSetRequestDto inputData)
        {
            var prompt = $@"
Generate a TOEIC practice quiz titled: '{inputData.Topic}'.
Description: Focus on TOEIC Part 1 , 
suitable for learners with TOEIC scores around {inputData.Difficulty}.
Question should describe a photo scene with one correct answer among four choices. 
The image should be described in detail with atleast 50 words.

Generate ONE question that matches this theme.


Only return in this structure no need any extended field/infor:
{{
  ""ImageDescription"": ""..."",
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
                Description = $"AI-generated TOEIC practice quiz on {inputData.Topic} focus on TOEIC part 1",
                QuizType = "Practice",
                SkillType = "",
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

                var audioScript = string.Join(Environment.NewLine,quiz.AnswerOptions.Select(o => $"{o.OptionLabel}. {o.OptionText}"));

                var audio = await GenerateAudioAsync(audioScript, VoiceRoles.Narrator);
                var image = await GenerateImageAsync(quiz.ImageDescription);

                var audioFile = await _uploadService.ConvertByteArrayToIFormFile(audio, $"audio-Q{i}-{inputData.CreatorId}-{DateTime.UtcNow}.mp3", "audio/mpeg");
                var imageFile = await _uploadService.ConvertByteArrayToIFormFile(image, $"image-Q{i}-{inputData.CreatorId}-{DateTime.UtcNow}.png", "image/png");

                var audioResult = await _uploadService.UploadAsync(audioFile);
                var imageResult = await _uploadService.UploadAsync(imageFile);

                var createdQuiz = await _quizService.CreateQuizAsync(new QuizRequestDto
                {
                    QuizSetId = createdQuizSet.Id,
                    QuestionText = quiz.QuestionText,
                    TOEICPart = "Part 1",
                    AudioURL = audioResult.Url,
                    ImageURL = imageResult.Url
                });

                foreach (var item in quiz.AnswerOptions)
                {
                    await _answerOptionService.CreateAsync(new RequestAnswerOptionDto
                    {
                        OptionLabel = item.OptionLabel,
                        OptionText = "",
                        IsCorrect = item.IsCorrect,
                        QuizId = createdQuiz.Id
                    });
                }
            }

            return createdQuizSet;
        }
        //Done
        public async Task<QuizSetResponseDto> GeneratePracticeQuizSetPart2Async(AiGenerateQuizSetRequestDto inputData)
        {
            var prompt = $@"
Generate a TOEIC practice quiz titled: '{inputData.Topic}'.
Description: Focus on TOEIC Part 2, 
suitable for learners with TOEIC scores around {inputData.Difficulty}.
Question have 3 answers, each answer should have different context then the correct one is the answer that fit with the topic, you have to generate question text, answer options. 

Generate ONE question that matches this theme.

Only return in this structure no need any extended field/infor:
{{
  ""QuestionText"": ""..."",
  ""AnswerOptions"": [
    {{ ""OptionLabel"": ""A"", ""OptionText"": ""..."", ""IsCorrect"": true/false }},
    {{ ""OptionLabel"": ""B"", ""OptionText"": ""..."", ""IsCorrect"": true/false }},
    {{ ""OptionLabel"": ""C"", ""OptionText"": ""..."", ""IsCorrect"": true/false }}
  ]
}}";

            var createdQuizSet = await _quizSetService.CreateQuizSetAsync(new QuizSetRequestDto
            {
                Title = inputData.Topic,
                Description = $"AI-generated TOEIC practice quiz on {inputData.Topic} focus on TOEIC part 2",
                QuizType = "Practice",
                SkillType = "",
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

                var audioScript = "Question: " + quiz.QuestionText + ".\n" + string.Join(Environment.NewLine, quiz.AnswerOptions.Select(o => $"{o.OptionLabel}. {o.OptionText}"));

                var audio = await GenerateAudioAsync(audioScript, VoiceRoles.Narrator);
                
                var audioFile = await _uploadService.ConvertByteArrayToIFormFile(audio, $"audio-Q{i}-{inputData.CreatorId}-{DateTime.UtcNow}.mp3", "audio/mpeg");
                
                var audioResult = await _uploadService.UploadAsync(audioFile);
                
                var createdQuiz = await _quizService.CreateQuizAsync(new QuizRequestDto
                {
                    QuizSetId = createdQuizSet.Id,
                    QuestionText = "",
                    TOEICPart = "Part 2",
                    AudioURL = audioResult.Url
                });

                foreach (var item in quiz.AnswerOptions)
                {
                    await _answerOptionService.CreateAsync(new RequestAnswerOptionDto
                    {
                        OptionLabel = item.OptionLabel,
                        OptionText = "",
                        IsCorrect = item.IsCorrect,
                        QuizId = createdQuiz.Id
                    });
                }
            }
            return createdQuizSet;
        }

        public Task<QuizSetResponseDto> GeneratePracticeQuizSetPart3Async(AiGenerateQuizSetRequestDto inputData)
        {
            throw new NotImplementedException();
        }

        public Task<QuizSetResponseDto> GeneratePracticeQuizSetPart4Async(AiGenerateQuizSetRequestDto inputData)
        {
            throw new NotImplementedException();
        }

        public async Task<QuizSetResponseDto> GeneratePracticeQuizSetPart5Async(AiGenerateQuizSetRequestDto inputData)
        {
            var prompt = $@"
Generate a TOEIC practice quiz titled: '{inputData.Topic}'.
Description: Focus on TOEIC Part 5 - in this part the question will be an incomplete sentence with 4 answer to fill in, 
suitable for learners with TOEIC scores around {inputData.Difficulty}.


Generate ONE question that matches this theme.

Only return in this structure no need any extended field/infor:
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
                Description = $"AI-generated TOEIC practice quiz on {inputData.Topic} focus on TOEIC part 5",
                QuizType = "Practice",
                SkillType = "",
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
                    TOEICPart = "Part 5",
                    CorrectAnswer = quiz.AnswerOptions.FirstOrDefault(o => o.IsCorrect)!.OptionLabel,
                });

                foreach (var item in quiz.AnswerOptions)
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

        public Task<QuizSetResponseDto> GeneratePracticeQuizSetPart6Async(AiGenerateQuizSetRequestDto inputData)
        {
            throw new NotImplementedException();
        }

        public Task<QuizSetResponseDto> GeneratePracticeQuizSetPart7Async(AiGenerateQuizSetRequestDto inputData)
        {
            throw new NotImplementedException();
        }

        public Task<(bool, string)> ValidateImageAsync(string context)
        {
            throw new NotImplementedException();
        }

        public Task<(bool, string)> ValidateAudioAsync(string script)
        {
            throw new NotImplementedException();
        }
    }
}
