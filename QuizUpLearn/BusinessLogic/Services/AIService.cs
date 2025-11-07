using BusinessLogic.DTOs;
using BusinessLogic.DTOs.AiDtos;
using BusinessLogic.DTOs.QuizDtos;
using BusinessLogic.DTOs.QuizGroupItemDtos;
using BusinessLogic.DTOs.QuizSetDtos;
using BusinessLogic.Helpers;
using BusinessLogic.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Repository.Enums;
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
        private readonly ILogger<AIService> _logger;
        private readonly IQuizGroupItemService _quizGroupItemService;
        //API keys
        private readonly string _geminiApiKey;
        private readonly string _openRouterApiKey;
        private readonly string _asyncAiApiKey;
        private readonly string _nebiusApiKey;
        //Voice IDs
        private readonly string _maleVoiceId;
        private readonly string _femaleVoiceId;
        private readonly string _narratorVoiceId;
        public AIService(HttpClient httpClient, IConfiguration configuration, IQuizSetService quizSetService, IQuizService quizService, IAnswerOptionService answerOptionService, IUploadService uploadService, ILogger<AIService> logger, IQuizGroupItemService quizGroupItemService)
        {
            _httpClient = httpClient;
            _quizSetService = quizSetService;
            _quizService = quizService;
            _answerOptionService = answerOptionService;
            _uploadService = uploadService;
            _logger = logger;
            _quizGroupItemService = quizGroupItemService;

            //API keys
            _geminiApiKey = configuration["Gemini:ApiKey"] ?? throw new ArgumentNullException("Gemini API key is not configured.");
            _openRouterApiKey = configuration["OpenRouter:ApiKey"] ?? throw new ArgumentNullException("Open router API key is not configured.");
            _asyncAiApiKey = configuration["AsyncTTS:ApiKey"] ?? throw new ArgumentNullException("OpenAITTS API key is not configured.");
            _nebiusApiKey = configuration["Nebius:ApiKey"] ?? throw new ArgumentNullException("Nebius API key is not configured.");

            //Voice ids
            _maleVoiceId = configuration["AsyncTTS:Voices:Male"] ?? throw new ArgumentNullException("Male voice id is not configured");
            _femaleVoiceId = configuration["AsyncTTS:Voices:Female"] ?? throw new ArgumentNullException("Female voice id is not configured"); ;
            _narratorVoiceId = configuration["AsyncTTS:Voices:Narrator"] ?? throw new ArgumentNullException("Narrator voice id is not configured");
            
        }

        public Task AnalyzeUserProgress()
        {
            throw new NotImplementedException();
        }

        private async Task<string> GeminiGenerateContentAsync(string prompt)
        {
            _logger.LogInformation($"Gemini api key: {_geminiApiKey}");
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
            using var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Add("x-goog-api-key", _geminiApiKey);
            request.Content = content;

            using var response = await _httpClient.SendAsync(request);
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
            _logger.LogInformation($"OpenRouter api key: {_openRouterApiKey}");
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

            using var response = await _httpClient.PostAsync(url, content);
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
            _logger.LogInformation($"Nebius api key: {_nebiusApiKey}");
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

            using var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _nebiusApiKey);
            request.Content = content;

            using var response = await _httpClient.SendAsync(request);
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

            var url = "https://api.async.ai/text_to_speech/streaming";

            var requestBody = new
            {
                model_id = "asyncflow_v2.0",
                transcript = text,
                voice = new {
                    mode = "id",
                    id = voiceId
                },
                output_format = new
                {
                    container = "mp3",
                    encoding = "pcm_s16le",
                    sample_rate = 44100
                }
            };

            using var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Add("X-Api-Key", _asyncAiApiKey);
            request.Content = new StringContent(
                JsonSerializer.Serialize(requestBody),
                Encoding.UTF8,
                "application/json"
            );

            using var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsByteArrayAsync();
        }

        private async Task<byte[]> GenerateConversationAudioAsync(AiConversationAudioScriptResponseDto conversation)
        {
            using var combinedStream = new MemoryStream();
            var silenceBytes = new byte[13230];

            foreach (var line in conversation.AudioScripts)
            {
                var lineAudioBytes = await GenerateAudioAsync(line.Text, line.Role);

                await combinedStream.WriteAsync(lineAudioBytes, 0, lineAudioBytes.Length);

                if (line != conversation.AudioScripts.Last())
                    await combinedStream.WriteAsync(silenceBytes, 0, silenceBytes.Length);
            }

            return combinedStream.ToArray();
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
            var groupPassage = string.Empty;
            var groupAudioScript = string.Empty;
            var groupImageDescription = string.Empty;

            foreach (var quiz in quizzes)
            {
                //Take group items
                if (quiz.QuizGroupItemId != null)
                {
                    var groupItems = await _quizGroupItemService.GetByIdAsync(quiz.QuizGroupItemId.Value);
                    if(groupItems != null)
                    {
                        groupPassage = groupItems.PassageText ?? string.Empty;
                        groupAudioScript = groupItems.AudioScript ?? string.Empty;
                        groupImageDescription = groupItems.ImageDescription ?? string.Empty;
                    }
                }
                var options = await _answerOptionService.GetByQuizIdAsync(quiz.Id);
                // Prepare validation prompt
                var validationPrompt = $@"
You are an expert TOEIC test validator.
Review this quiz for correctness and clarity.

### Quiz Set Context:
TOEIC practice quiz titled: '{quizSet.Title}'.
Description: {quizSet.Description}, 
Toeic part: {quiz.TOEICPart},
suitable for learners with TOEIC scores around {quizSet.DifficultyLevel}.

### Quiz to Validate
Additional info(if any):
- Passage: {groupPassage}
- Audio script: {groupAudioScript}
- Image description: {groupImageDescription}

Question: {quiz.QuestionText}

Options:
{string.Join("\n", options.Select(o => $"{o.OptionLabel}. {o.OptionText} (Correct: {o.IsCorrect})"))}

Check the criteria and explain shortly at Feedback field if the question is invalid:
1. The question is grammatically correct and meaningful.
2. There is ONE or more correct answer.
3. The correct answer makes sense in context.
4. Not duplicating or very similar options.
5. No need suggestion improvement, only validate correctness.

Return only these 2 fields as JSON structure:
{{ 
    ""IsValid"": true/false,
    ""Feedback"": "".....""
}}
";
                var response = await GenerateContentAsync(validationPrompt);
                
                var validation = JsonSerializer.Deserialize<AiValidationResponseDto>(response ?? string.Empty);

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
        
        public async Task<QuizSetResponseDto> GeneratePracticeQuizSetPart1Async(AiGenerateQuizSetRequestDto inputData)
        {
            var createdQuizSet = await _quizSetService.CreateQuizSetAsync(new QuizSetRequestDto
            {
                Title = inputData.Topic,
                Description = $"AI-generated TOEIC practice quiz on {inputData.Topic} focus on TOEIC part 1",
                QuizType = QuizSetTypeEnum.Practice,
                SkillType = "Listening",
                DifficultyLevel = inputData.Difficulty,
                CreatedBy = inputData.CreatorId
            });
            string previousImageDescription = string.Empty;
            string previousQuestionText = string.Empty;

            for (int i = 0; i < inputData.QuestionQuantity; i++)
            {
                var prompt = $@"
Generate a TOEIC practice quiz titled: '{inputData.Topic}'.
Description: Focus on TOEIC Part 1 , 
suitable for learners with TOEIC scores around {inputData.Difficulty}.
Question should describe a photo scene with one correct answer among four choices. 
The image should be described in detail with atleast 50 words.

Avoid previous image description(if any): {previousImageDescription}

Generate ONE question that matches this theme.

Avoid previous question text(if any): {previousQuestionText}

Need to return 3 field:
- ImageDescription: A detailed description of the image related to the question.
- QuestionText: The question text.
- AnswerOptions: List of 4 answer options with labels and correctness. Each option must have option label(A/B/C/D), option text, isCorrect(true/false).

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
                var response = await GeminiGenerateContentAsync(prompt);
                previousImageDescription += response + "\n";

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

                previousQuestionText += quiz.QuestionText + ", ";
            }

            return createdQuizSet;
        }
        
        public async Task<QuizSetResponseDto> GeneratePracticeQuizSetPart2Async(AiGenerateQuizSetRequestDto inputData)
        {
            var createdQuizSet = await _quizSetService.CreateQuizSetAsync(new QuizSetRequestDto
            {
                Title = inputData.Topic,
                Description = $"AI-generated TOEIC practice quiz on {inputData.Topic} focus on TOEIC part 2",
                QuizType = QuizSetTypeEnum.Practice,
                SkillType = "Listening",
                DifficultyLevel = inputData.Difficulty,
                CreatedBy = inputData.CreatorId
            });
            string previousQuestionText = string.Empty;
            for (int i = 0; i < inputData.QuestionQuantity; i++)
            {
                var prompt = $@"
Generate a TOEIC practice quiz titled: '{inputData.Topic}'.
Description: Focus on TOEIC Part 2, 
suitable for learners with TOEIC scores around {inputData.Difficulty}.
Question have 3 answers, each answer should have different context then the correct one is the answer that fit with the topic, you have to generate question text, answer options. 

Generate ONE question that matches this theme.

Avoid previous question text(if any): {previousQuestionText}

Need to return 2 field:
- QuestionText: The question text.
- AnswerOptions: List of 4 answer options with labels and correctness. Each option must have option label(A/B/C/D), option text, isCorrect(true/false).

Only return in this structure no need any extended field/infor:
{{
  ""QuestionText"": ""..."",
  ""AnswerOptions"": [
    {{ ""OptionLabel"": ""A"", ""OptionText"": ""..."", ""IsCorrect"": true/false }},
    {{ ""OptionLabel"": ""B"", ""OptionText"": ""..."", ""IsCorrect"": true/false }},
    {{ ""OptionLabel"": ""C"", ""OptionText"": ""..."", ""IsCorrect"": true/false }}
  ]
}}";
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

                previousQuestionText += quiz.QuestionText + ", ";
            }
            return createdQuizSet;
        }
        
        public async Task<QuizSetResponseDto> GeneratePracticeQuizSetPart3Async(AiGenerateQuizSetRequestDto inputData)
        {
            var createdQuizSet = await _quizSetService.CreateQuizSetAsync(new QuizSetRequestDto
            {
                Title = inputData.Topic,
                Description = $"AI-generated TOEIC practice quiz on {inputData.Topic} focus on TOEIC part 3",
                QuizType = QuizSetTypeEnum.Practice,
                SkillType = "Listening",
                DifficultyLevel = inputData.Difficulty,
                CreatedBy = inputData.CreatorId
            });
            Guid quizSetId = createdQuizSet.Id;
            var previousAudioScript = string.Empty;

            for (int i = 0; i < inputData.QuestionQuantity; i += 3)
            {
                var audioPrompt = $@"
Generate a TOEIC audio topic: '{inputData.Topic}'.
Description: Focus on TOEIC Part 3, 
suitable for learners with TOEIC scores around {inputData.Difficulty}.

Avoid the previous audio script(if it is not null): {previousAudioScript}

Generate ONE generate audio script that contain a short conservation between TWO people (different gender) and matches this theme.
The audio must have 3-5 exchanges (each person speak 2-3 times) and each exchange should have 15-30 words (must have 2-3 sentences in 1 exchange).

Only return in this structure:
{{
  ""AudioScripts"": [
    {{ ""Role"": ""Male"", ""Text"": ""..."" }},
    {{ ""Role"": ""Female"", ""Text"": ""..."" }},
    {{ ""Role"": ""Male"", ""Text"": ""Where are you going?"" }},
    {{ ""Role"": ""Female"", ""Text"": ""To the office."" }},
......
]
}}";
                var audioResponse = await GeminiGenerateContentAsync(audioPrompt);
                previousAudioScript = string.Join("\n", audioResponse);
                var conversationScripts = JsonSerializer.Deserialize<AiConversationAudioScriptResponseDto>(audioResponse);
                var audioBytes = await GenerateConversationAudioAsync(conversationScripts!);
                var audioFile = await _uploadService.ConvertByteArrayToIFormFile(audioBytes, $"audio-G3_{i / 3 + 1}-{inputData.CreatorId}-{DateTime.UtcNow}.mp3", "audio/mpeg");
                var audioResult = await _uploadService.UploadAsync(audioFile);

                var groupItem = await _quizGroupItemService.CreateAsync(new RequestQuizGroupItemDto
                {
                    QuizSetId = quizSetId,
                    Name = $"Group3_{i / 3 + 1}",
                    AudioUrl = audioResult.Url,
                    AudioScript = audioResponse
                });

                string previousQuizText = string.Empty;
                for (int j = 0; j < 3 && i + j < inputData.QuestionQuantity; j++)
                {
                    var quizPrompt = $@"
Based on this audio script: {audioResponse}
Generate ONE TOEIC Part 3 question with 3 wrong answers and 1 correct answer.

Avoid the previous question text(if it is not null): {previousQuizText}

Need to return 2 field:
- QuestionText: The question text.
- AnswerOptions: List of 4 answer options with labels and correctness. Each option must have option label(A/B/C/D), option text, isCorrect(true/false).

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
                    var response = await GeminiGenerateContentAsync(quizPrompt);

                    var quiz = JsonSerializer.Deserialize<AiGenerateQuizResponseDto>(response);
                    if (quiz == null
                        || string.IsNullOrEmpty(quiz.QuestionText)
                        || quiz.AnswerOptions.Count == 0)
                        throw new Exception("Failed to generate valid quiz data from AI.");

                    previousQuizText += quiz.QuestionText + ", ";

                    var createdQuiz = await _quizService.CreateQuizAsync(new QuizRequestDto
                    {
                        QuizSetId = createdQuizSet.Id,
                        QuestionText = quiz.QuestionText,
                        TOEICPart = "Part 3",
                        QuizGroupItemId = groupItem.Id
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
            }
            return createdQuizSet;
        }
        
        public async Task<QuizSetResponseDto> GeneratePracticeQuizSetPart4Async(AiGenerateQuizSetRequestDto inputData)
        {
            var createdQuizSet = await _quizSetService.CreateQuizSetAsync(new QuizSetRequestDto
            {
                Title = inputData.Topic,
                Description = $"AI-generated TOEIC practice quiz on {inputData.Topic} focus on TOEIC part 4",
                QuizType = QuizSetTypeEnum.Practice,
                SkillType = "Listening",
                DifficultyLevel = inputData.Difficulty,
                CreatedBy = inputData.CreatorId
            });

            Guid quizSetId = createdQuizSet.Id;
            string previousAudioScript = string.Empty;
            
            for (int i = 0; i < inputData.QuestionQuantity; i += 3)
            {
                var audioPrompt = $@"
Generate a TOEIC audio topic: '{inputData.Topic}'.
Description: Focus on TOEIC Part 4, 
suitable for learners with TOEIC scores around {inputData.Difficulty}.

Avoid the previous audio script(if it is not null): {previousAudioScript}

Generate ONE audio script that contain a short monologue (announcement/speech) matches this theme.

Only return in this structure:
{{
  ""AudioScript"": ""...""
}}";
                var audioResponse = await GeminiGenerateContentAsync(audioPrompt);
                
                var audioScripts = JsonSerializer.Deserialize<AiGenerateQuizResponseDto>(audioResponse);
                if(audioScripts == null 
                    || string.IsNullOrEmpty(audioScripts.AudioScript))
                    throw new Exception("Failed to generate valid audio script from AI.");

                previousAudioScript = string.Join("\n", audioScripts.AudioScript);

                var audioBytes = await GenerateAudioAsync(audioScripts.AudioScript!, VoiceRoles.Narrator);
                var audioFile = await _uploadService.ConvertByteArrayToIFormFile(audioBytes, $"audio-G4_{i / 3 + 1}-{inputData.CreatorId}-{DateTime.UtcNow}.mp3", "audio/mpeg");
                var audioResult = await _uploadService.UploadAsync(audioFile);

                var groupItem = await _quizGroupItemService.CreateAsync(new RequestQuizGroupItemDto
                {
                    QuizSetId = quizSetId,
                    Name = $"Group4_{i / 3 + 1}",
                    AudioUrl = audioResult.Url,
                    AudioScript = audioScripts.AudioScript
                });

                string previousQuizText = string.Empty;
                for (int j = 0; j < 3 && i + j < inputData.QuestionQuantity; j++)
                {
                    var quizPrompt = $@"
Based on this audio script: {audioResponse}
Generate ONE TOEIC Part 4 question with 3 wrong answers and 1 correct answer.

Avoid the previous question text(if it is not null): {previousQuizText}

Need to return 2 field:
- QuestionText: The question text.
- AnswerOptions: List of 4 answer options with labels and correctness. Each option must have option label(A/B/C/D), option text, isCorrect(true/false).

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
                    var response = await GeminiGenerateContentAsync(quizPrompt);

                    var quiz = JsonSerializer.Deserialize<AiGenerateQuizResponseDto>(response);
                    if (quiz == null
                        || string.IsNullOrEmpty(quiz.QuestionText)
                        || quiz.AnswerOptions.Count == 0)
                        throw new Exception("Failed to generate valid quiz data from AI.");

                    previousQuizText += quiz.QuestionText + ", ";

                    var createdQuiz = await _quizService.CreateQuizAsync(new QuizRequestDto
                    {
                        QuizSetId = createdQuizSet.Id,
                        QuestionText = quiz.QuestionText,
                        TOEICPart = "Part 3",
                        QuizGroupItemId = groupItem.Id
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
            }
            return createdQuizSet;
        }
        
        public async Task<QuizSetResponseDto> GeneratePracticeQuizSetPart5Async(AiGenerateQuizSetRequestDto inputData)
        {
            var createdQuizSet = await _quizSetService.CreateQuizSetAsync(new QuizSetRequestDto
            {
                Title = inputData.Topic,
                Description = $"AI-generated TOEIC practice quiz on {inputData.Topic} focus on TOEIC part 5",
                QuizType = QuizSetTypeEnum.Practice,
                SkillType = "Reading",
                DifficultyLevel = inputData.Difficulty,
                CreatedBy = inputData.CreatorId
            });

            string previousQuestionText = string.Empty;

            for (int i = 0; i < inputData.QuestionQuantity; i++)
            {
                var prompt = $@"
Generate a TOEIC practice quiz titled: '{inputData.Topic}'.
Description: Focus on TOEIC Part 5 - in this part the question will be an incomplete sentence with 4 answer to fill in, 
suitable for learners with TOEIC scores around {inputData.Difficulty}.

Generate ONE question that matches this theme.

Avoid previous question text(if any): {previousQuestionText}

Need to return 2 field:
- QuestionText: The question text.
- AnswerOptions: List of 4 answer options with labels and correctness. Each option must have option label(A/B/C/D), option text, isCorrect(true/false).

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

                previousQuestionText += quiz.QuestionText + ", ";
            }
            return createdQuizSet;
        }

        public async Task<QuizSetResponseDto> GeneratePracticeQuizSetPart6Async(AiGenerateQuizSetRequestDto inputData)
        {
            var createdQuizSet = await _quizSetService.CreateQuizSetAsync(new QuizSetRequestDto
            {
                Title = inputData.Topic,
                Description = $"AI-generated TOEIC practice quiz on {inputData.Topic} focus on TOEIC part 6",
                QuizType = QuizSetTypeEnum.Practice,
                SkillType = "Reading",
                DifficultyLevel = inputData.Difficulty,
                CreatedBy = inputData.CreatorId
            });

            Guid quizSetId = createdQuizSet.Id;
            string previousPassages = string.Empty;

            for (int i = 0; i < inputData.QuestionQuantity; i += 4)
            {
                var passagePrompt = $@"
Generate a TOEIC passage: '{inputData.Topic}'.
Description: Focus on TOEIC Part 6, 
suitable for learners with TOEIC scores around {inputData.Difficulty}.

Avoid the previous passage(if it is not null): {previousPassages}

Generate ONE passage match the theme - generate fully passage without any blanks (at least 75 words with 6 sentences).

Only return in this structure:
{{
  ""Passage"": ""..."",
}}";
                var passageResponse = await GeminiGenerateContentAsync(passagePrompt);

                var passageResult = JsonSerializer.Deserialize<AiGenerateQuizResponseDto>(passageResponse);
                if (passageResult == null
                    || string.IsNullOrEmpty(passageResult.Passage))
                    throw new Exception("Failed to generate valid passage from AI.");

                previousPassages = string.Join("\n", passageResult.Passage);
                var groupItem = await _quizGroupItemService.CreateAsync(new RequestQuizGroupItemDto
                {
                    QuizSetId = quizSetId,
                    Name = $"Group6_{i / 4 + 1}",
                    PassageText = passageResult.Passage
                });

                var usedBlanks = string.Empty;

                for (int j = 1; j <= 4 && i + j <= inputData.QuestionQuantity; j++)
                {
                    var quizPrompt = $@"
Based on this passage: {passageResult.Passage}
Replace the {j}th important word with a blank marked as ({j}).

Avoid using these previous blanks(if any): [{string.Join(", ", usedBlanks)}]

Return the question text with ({j}) and 4 answer options (1 correct, 3 wrong). Return the modified passage as 'QuestionText'

Need to return 2 field:
- QuestionText: The question text.
- AnswerOptions: List of 4 answer options with labels and correctness. Each option must have option label(A/B/C/D), option text, isCorrect(true/false).

Only return in this structure no need any extended field/infor:
{{
  ""QuestionText"": """",
  ""AnswerOptions"": [
    {{ ""OptionLabel"": ""A"", ""OptionText"": ""..."", ""IsCorrect"": true/false }},
    {{ ""OptionLabel"": ""B"", ""OptionText"": ""..."", ""IsCorrect"": true/false }},
    {{ ""OptionLabel"": ""C"", ""OptionText"": ""..."", ""IsCorrect"": true/false }},
    {{ ""OptionLabel"": ""D"", ""OptionText"": ""..."", ""IsCorrect"": true/false }}
  ]
}}";
                    var response = await GeminiGenerateContentAsync(quizPrompt);

                    var quiz = JsonSerializer.Deserialize<AiGenerateQuizResponseDto>(response);
                    if (quiz == null
                        || string.IsNullOrEmpty(quiz.QuestionText)
                        || quiz.AnswerOptions.Count == 0)
                        throw new Exception("Failed to generate valid quiz data from AI.");
                    //create quiz
                    var createdQuiz = await _quizService.CreateQuizAsync(new QuizRequestDto
                    {
                        QuizSetId = createdQuizSet.Id,
                        QuestionText = j.ToString(),
                        TOEICPart = "Part 6",
                        QuizGroupItemId = groupItem.Id
                    });
                    //Create answer options
                    foreach (var item in quiz.AnswerOptions)
                    {
                        await _answerOptionService.CreateAsync(new RequestAnswerOptionDto
                        {
                            OptionLabel = item.OptionLabel,
                            OptionText = item.OptionText,
                            IsCorrect = item.IsCorrect,
                            QuizId = createdQuiz.Id
                        });

                        if (item.IsCorrect)
                        {
                            usedBlanks += $"{j}." + item.OptionText + ", ";
                        }
                    }
                    //update passage with the latest blank
                    passageResult.Passage = quiz.QuestionText;
                }
                //Update the last passage with all blanks
                await _quizGroupItemService.UpdateAsync(groupItem.Id, new RequestQuizGroupItemDto
                {
                    PassageText = passageResult.Passage
                });

            }


            return createdQuizSet;
        }

        public async Task<QuizSetResponseDto> GeneratePracticeQuizSetPart7Async(AiGenerateQuizSetRequestDto inputData)
        {
            var createdQuizSet = await _quizSetService.CreateQuizSetAsync(new QuizSetRequestDto
            {
                Title = inputData.Topic,
                Description = $"AI-generated TOEIC Part 7 Reading practice on {inputData.Topic}",
                QuizType = QuizSetTypeEnum.Practice,
                SkillType = "Reading",
                DifficultyLevel = inputData.Difficulty,
                CreatedBy = inputData.CreatorId
            });

            Guid quizSetId = createdQuizSet.Id;
            string previousPassages = string.Empty;

            for (int i = 0; i < inputData.QuestionQuantity; i += 3)
            {
                var passagePrompt = $@"
Generate a TOEIC Part 7 reading passage about '{inputData.Topic}' 
for learners with TOEIC score around {inputData.Difficulty}.
Avoid repeating previous passages: {previousPassages}.
Length: around 120-150 words.

Return only JSON:
{{
  ""Passage"": ""...""
}}";

                var passageResponse = await GeminiGenerateContentAsync(passagePrompt);
                var passageResult = JsonSerializer.Deserialize<AiGenerateQuizResponseDto>(passageResponse);

                if (passageResult == null || string.IsNullOrEmpty(passageResult.Passage))
                    throw new Exception("Failed to generate valid passage.");

                previousPassages = passageResult.Passage;

                var groupItem = await _quizGroupItemService.CreateAsync(new RequestQuizGroupItemDto
                {
                    QuizSetId = quizSetId,
                    Name = $"Group7_{i / 3 + 1}",
                    PassageText = passageResult.Passage
                });

                string previousQuizText = string.Empty;
                for (int j = 1; j <= 3 && i + j <= inputData.QuestionQuantity; j++)
                {
                    var quizPrompt = $@"
Based on this TOEIC passage: {passageResult.Passage}
Generate ONE reading comprehension question (like TOEIC Part 7).
Include 4 options (A–D) with 1 correct answer.

Avoid previous question text(if any): {previousQuizText}
Need to return 2 field:
- QuestionText: The question text.
- AnswerOptions: List of 4 answer options with labels and correctness. Each option must have option label(A/B/C/D), option text, isCorrect(true/false).

Return JSON:
{{
  ""QuestionText"": ""..."",
  ""AnswerOptions"": [
    {{ ""OptionLabel"": ""A"", ""OptionText"": ""..."", ""IsCorrect"": true/false }},
    {{ ""OptionLabel"": ""B"", ""OptionText"": ""..."", ""IsCorrect"": true/false }},
    {{ ""OptionLabel"": ""C"", ""OptionText"": ""..."", ""IsCorrect"": true/false }},
    {{ ""OptionLabel"": ""D"", ""OptionText"": ""..."", ""IsCorrect"": true/false }}
  ]
}}";
                    var response = await GeminiGenerateContentAsync(quizPrompt);
                    var quiz = JsonSerializer.Deserialize<AiGenerateQuizResponseDto>(response);

                    if (quiz == null || string.IsNullOrEmpty(quiz.QuestionText))
                        throw new Exception("Invalid quiz data.");

                    previousQuizText += quiz.QuestionText + ", ";

                    var createdQuiz = await _quizService.CreateQuizAsync(new QuizRequestDto
                    {
                        QuizSetId = createdQuizSet.Id,
                        QuestionText = quiz.QuestionText,
                        TOEICPart = "Part 7",
                        QuizGroupItemId = groupItem.Id
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
            }

            return createdQuizSet;
        }
    }
}
