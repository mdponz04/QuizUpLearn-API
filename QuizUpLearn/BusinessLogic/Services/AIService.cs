using BusinessLogic.DTOs;
using BusinessLogic.DTOs.AiDtos;
using BusinessLogic.DTOs.QuizDtos;
using BusinessLogic.DTOs.QuizGroupItemDtos;
using BusinessLogic.DTOs.QuizSetDtos;
using BusinessLogic.DTOs.UserMistakeDtos;
using BusinessLogic.DTOs.UserWeakPointDtos;
using BusinessLogic.Helpers;
using BusinessLogic.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
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
        private readonly IUserMistakeService _userMistakeService;
        private readonly IUserWeakPointService _userWeakPointService;
        //API keys
        private readonly string _geminiApiKey;
        private readonly string _openRouterApiKey;
        private readonly string _asyncAiApiKey;
        private readonly string _nebiusApiKey;
        //Voice IDs
        private readonly string _maleVoiceId;
        private readonly string _femaleVoiceId;
        private readonly string _narratorVoiceId;
        //Helpers
        private readonly PromptGenerateQuizSetHelper _promptGenerator;
        public AIService(HttpClient httpClient, IConfiguration configuration, IQuizSetService quizSetService, IQuizService quizService, IAnswerOptionService answerOptionService, IUploadService uploadService, ILogger<AIService> logger, IQuizGroupItemService quizGroupItemService, IUserMistakeService userMistakeService, IUserWeakPointService userWeakPointService)
        {
            _httpClient = httpClient;
            _quizSetService = quizSetService;
            _quizService = quizService;
            _answerOptionService = answerOptionService;
            _uploadService = uploadService;
            _logger = logger;
            _quizGroupItemService = quizGroupItemService;
            _userMistakeService = userMistakeService;
            _userWeakPointService = userWeakPointService;

            //API keys
            _geminiApiKey = configuration["Gemini:ApiKey"] ?? throw new ArgumentNullException("Gemini API key is not configured.");
            _openRouterApiKey = configuration["OpenRouter:ApiKey"] ?? throw new ArgumentNullException("Open router API key is not configured.");
            _asyncAiApiKey = configuration["AsyncTTS:ApiKey"] ?? throw new ArgumentNullException("OpenAITTS API key is not configured.");
            _nebiusApiKey = configuration["Nebius:ApiKey"] ?? throw new ArgumentNullException("Nebius API key is not configured.");

            //Voice ids
            _maleVoiceId = configuration["AsyncTTS:Voices:Male"] ?? throw new ArgumentNullException("Male voice id is not configured");
            _femaleVoiceId = configuration["AsyncTTS:Voices:Female"] ?? throw new ArgumentNullException("Female voice id is not configured"); ;
            _narratorVoiceId = configuration["AsyncTTS:Voices:Narrator"] ?? throw new ArgumentNullException("Narrator voice id is not configured");

            _promptGenerator = new PromptGenerateQuizSetHelper();
        }

        private async Task<string> GeminiGenerateContentAsync(string prompt)
        {
            var url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash-lite:generateContent";
            /*Console.WriteLine("Http client Gemini base address: " + _httpClient.BaseAddress);
            Console.WriteLine("Gemini api key: " + _geminiApiKey);*/

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

        private async Task<string?> OpenRouterGenerateContentAsync(string prompt)
        {
            var url = "https://openrouter.ai/api/v1/chat/completions";
            /*Console.WriteLine("Http client open router base address: " + _httpClient.BaseAddress);
            Console.WriteLine("Open router api key: " + _openRouterApiKey);*/

            var body = new
            {
                model = "x-ai/grok-4.1-fast",
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
            using var content = new StringContent(json, Encoding.UTF8, "application/json");
            // Attach the API key per request instead of modifying DefaultRequestHeaders
            using var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = content
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _openRouterApiKey);

            using var response = await _httpClient.SendAsync(request);
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

            var quizzes = await _quizService.GetQuizzesByQuizSetIdAsync(quizSetId, null!);
            if (quizzes == null)
                return (false, "No quizzes found in this set.");

            bool allValid = true;
            var feedbackBuilder = new StringBuilder();
            var groupPassage = string.Empty;
            var groupAudioScript = string.Empty;
            var groupImageDescription = string.Empty;

            foreach (var quiz in quizzes.Data)
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
                var validationPrompt = _promptGenerator.GetValidationPromptAsync(quizSet, quiz, quiz.AnswerOptions, groupPassage, groupAudioScript, groupImageDescription);
                var response = await OpenRouterGenerateContentAsync(validationPrompt);
                
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

            if (!allValid)
                await _quizSetService.HardDeleteQuizSetAsync(quizSetId);

            return (allValid, feedbackBuilder.ToString());
        }
        
        public async Task<bool> GeneratePracticeQuizSetPart1Async(AiGenerateQuizSetRequestDto inputData, Guid quizSetId)
        {
            string previousImageDescription = string.Empty;
            string previousQuestionText = string.Empty;

            for (int i = 0; i < inputData.QuestionQuantity; i++)
            {
                var prompt = _promptGenerator.GetQuizSetPart1Prompt(inputData, previousImageDescription, previousQuestionText);
                var response = await GeminiGenerateContentAsync(prompt);
                

                AiGenerateQuizResponseDto? quiz;
                try
                {
                    quiz = JsonSerializer.Deserialize<AiGenerateQuizResponseDto>(response);
                    if (quiz == null
                        || string.IsNullOrEmpty(quiz.QuestionText)
                        || quiz.AnswerOptions == null
                        || quiz.ImageDescription == null
                        || quiz.AnswerOptions.Count == 0)
                        throw new JsonException("Failed to generate valid quiz data from AI.");

                    previousImageDescription += quiz.ImageDescription + "\n";
                }
                catch(Exception ex)
                {
                    Console.WriteLine($"Invalid AI JSON: {ex.Message}");
                    i--;
                    continue;
                }

                var audioScript = string.Join(Environment.NewLine,quiz.AnswerOptions.Select(o => $"{o.OptionLabel}. {o.OptionText}"));

                var audio = await GenerateAudioAsync(audioScript, VoiceRoles.Narrator);
                var image = await GenerateImageAsync(quiz.ImageDescription);

                var audioFile = await _uploadService.ConvertByteArrayToIFormFile(audio, $"audio-Q{i}-{inputData.CreatorId}-{DateTime.UtcNow}.mp3", "audio/mpeg");
                var imageFile = await _uploadService.ConvertByteArrayToIFormFile(image, $"image-Q{i}-{inputData.CreatorId}-{DateTime.UtcNow}.png", "image/png");

                var audioResult = await _uploadService.UploadAsync(audioFile);
                var imageResult = await _uploadService.UploadAsync(imageFile);

                var groupItem = await _quizGroupItemService.CreateAsync(new RequestQuizGroupItemDto
                {
                    QuizSetId = quizSetId,
                    Name = $"Single quiz {QuizPartEnums.PART1.ToString()}",
                    AudioUrl = audioResult.Url,
                    AudioScript = audioScript,
                    ImageDescription = quiz.ImageDescription,
                    ImageUrl = imageResult.Url
                });

                if (groupItem == null)
                {
                    Console.WriteLine("Failed to create quiz group item.");
                    i--;
                    continue;
                }

                var createdQuiz = await _quizService.CreateQuizAsync(new QuizRequestDto
                {
                    QuizSetId = quizSetId,
                    QuestionText = quiz.QuestionText,
                    TOEICPart = QuizPartEnums.PART1.ToString(),
                    AudioURL = audioResult.Url,
                    ImageURL = imageResult.Url,
                    QuizGroupItemId = groupItem.Id
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

            return true;
        }
        
        public async Task<bool> GeneratePracticeQuizSetPart2Async(AiGenerateQuizSetRequestDto inputData, Guid quizSetId)
        {
            string previousQuestionText = string.Empty;
            for (int i = 0; i < inputData.QuestionQuantity; i++)
            {
                var prompt = _promptGenerator.GetQuizSetPart2Prompt(inputData, previousQuestionText);

                var response = await GeminiGenerateContentAsync(prompt);
                AiGenerateQuizResponseDto? quiz;
                try
                {
                    quiz = JsonSerializer.Deserialize<AiGenerateQuizResponseDto>(response);
                    if (quiz == null
                    || string.IsNullOrEmpty(quiz.QuestionText)
                    || quiz.AnswerOptions == null
                    || quiz.AnswerOptions.Count == 0)
                        throw new JsonException("Failed to generate valid quiz data from AI.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Invalid AI JSON: {ex.Message}");
                    i--;
                    continue;
                }

                var audioScript = "Question: " + quiz.QuestionText + ".\n" + string.Join(Environment.NewLine, quiz.AnswerOptions.Select(o => $"{o.OptionLabel}. {o.OptionText}"));

                var audio = await GenerateAudioAsync(audioScript, VoiceRoles.Narrator);
                
                var audioFile = await _uploadService.ConvertByteArrayToIFormFile(audio, $"audio-Q{i}-{inputData.CreatorId}-{DateTime.UtcNow}.mp3", "audio/mpeg");
                
                var audioResult = await _uploadService.UploadAsync(audioFile);

                var groupItem = await _quizGroupItemService.CreateAsync(new RequestQuizGroupItemDto
                {
                    QuizSetId = quizSetId,
                    Name = $"Single quiz {QuizPartEnums.PART2.ToString()}",
                    AudioUrl = audioResult.Url,
                    AudioScript = audioScript
                });

                if (groupItem == null)
                {
                    Console.WriteLine("Failed to create quiz group item.");
                    i--;
                    continue;
                }
                var createdQuiz = await _quizService.CreateQuizAsync(new QuizRequestDto
                {
                    QuizSetId = quizSetId,
                    QuestionText = "",
                    TOEICPart = QuizPartEnums.PART2.ToString(),
                    AudioURL = audioResult.Url,
                    QuizGroupItemId = groupItem.Id
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
            return true;
        }
        
        public async Task<bool> GeneratePracticeQuizSetPart3Async(AiGenerateQuizSetRequestDto inputData, Guid quizSetId)
        {
            var previousAudioScript = string.Empty;

            for (int i = 0; i < inputData.QuestionQuantity; i += 3)
            {
                var audioPrompt = _promptGenerator.GetPart3AudioPrompt(inputData, previousAudioScript);
                var audioResponse = await GeminiGenerateContentAsync(audioPrompt);
                AiConversationAudioScriptResponseDto? conversationScripts;
                try
                {
                    conversationScripts = JsonSerializer.Deserialize<AiConversationAudioScriptResponseDto>(audioResponse);
                    if(conversationScripts == null
                        || conversationScripts.AudioScripts.Count == 0)
                        throw new JsonException("Failed to generate valid audio script from AI.");
                }
                catch(Exception ex)
                {
                    Console.WriteLine($"Invalid AI JSON: {ex.Message}");
                    i--;
                    continue;
                }
                previousAudioScript = string.Join("\n", audioResponse);

                var audioBytes = await GenerateConversationAudioAsync(conversationScripts);
                var audioFile = await _uploadService.ConvertByteArrayToIFormFile(audioBytes, $"audio-G3_{i / 3 + 1}-{inputData.CreatorId}-{DateTime.UtcNow}.mp3", "audio/mpeg");
                var audioResult = await _uploadService.UploadAsync(audioFile);

                var groupItem = await _quizGroupItemService.CreateAsync(new RequestQuizGroupItemDto
                {
                    QuizSetId = quizSetId,
                    Name = $"Group3_{i / 3 + 1}",
                    AudioUrl = audioResult.Url,
                    AudioScript = audioResponse
                });

                if(groupItem == null)
                {
                    Console.WriteLine("Failed to create quiz group item.");
                    i -= 3;
                    continue;
                }

                string previousQuizText = string.Empty;
                for (int j = 0; j < 3 && i + j < inputData.QuestionQuantity; j++)
                {
                    var quizPrompt = _promptGenerator.GetPart3QuizPrompt(audioResponse, previousQuizText);
                    var response = await GeminiGenerateContentAsync(quizPrompt);
                    AiGenerateQuizResponseDto? quiz;
                    try
                    {
                        quiz = JsonSerializer.Deserialize<AiGenerateQuizResponseDto>(response);
                        if (quiz == null
                            || string.IsNullOrEmpty(quiz.QuestionText)
                            || quiz.AnswerOptions == null
                            || quiz.AnswerOptions.Count == 0)
                            throw new Exception("Failed to generate valid quiz data from AI.");
                    }
                    catch( Exception ex)
                    {
                        Console.WriteLine($"Invalid AI JSON: {ex.Message}");
                        j--;
                        continue;
                    }

                    previousQuizText += quiz.QuestionText + ", ";

                    var createdQuiz = await _quizService.CreateQuizAsync(new QuizRequestDto
                    {
                        QuizSetId = quizSetId,
                        QuestionText = quiz.QuestionText,
                        TOEICPart = QuizPartEnums.PART3.ToString(),
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
            return true;
        }
        
        public async Task<bool> GeneratePracticeQuizSetPart4Async(AiGenerateQuizSetRequestDto inputData, Guid quizSetId)
        {
            string previousAudioScript = string.Empty;
            
            for (int i = 0; i < inputData.QuestionQuantity; i += 3)
            {
                var audioPrompt = _promptGenerator.GetPart4AudioPrompt(inputData, previousAudioScript);
                var audioResponse = await GeminiGenerateContentAsync(audioPrompt);
                AiGenerateQuizResponseDto? audio = new();
                try
                {
                    audio = JsonSerializer.Deserialize<AiGenerateQuizResponseDto>(audioResponse);
                    if (audio == null || string.IsNullOrEmpty(audio.AudioScript))
                        throw new Exception("Failed to generate valid audio script from AI.");
                }
                catch(Exception ex)
                {
                    Console.WriteLine($"Invalid AI JSON: {ex.Message}");
                    i--;
                    continue;
                }
                

                previousAudioScript = string.Join("\n", audio.AudioScript);

                var audioBytes = await GenerateAudioAsync(audio.AudioScript!, VoiceRoles.Narrator);
                var audioFile = await _uploadService.ConvertByteArrayToIFormFile(audioBytes, $"audio-G4_{i / 3 + 1}-{inputData.CreatorId}-{DateTime.UtcNow}.mp3", "audio/mpeg");
                var audioResult = await _uploadService.UploadAsync(audioFile);

                var groupItem = await _quizGroupItemService.CreateAsync(new RequestQuizGroupItemDto
                {
                    QuizSetId = quizSetId,
                    Name = $"Group4_{i / 3 + 1}",
                    AudioUrl = audioResult.Url,
                    AudioScript = audio.AudioScript
                });

                if(groupItem == null)
                {
                    Console.WriteLine("Failed to create quiz group item.");
                    i -= 3;
                    continue;
                }

                string previousQuizText = string.Empty;
                for (int j = 0; j < 3 && i + j < inputData.QuestionQuantity; j++)
                {
                    var quizPrompt = _promptGenerator.GetPart4QuizPrompt(audio.AudioScript, previousQuizText);
                    var response = await GeminiGenerateContentAsync(quizPrompt);
                    AiGenerateQuizResponseDto? quiz = new();
                    try
                    {
                        quiz = JsonSerializer.Deserialize<AiGenerateQuizResponseDto>(response);
                        if (quiz == null
                            || string.IsNullOrEmpty(quiz.QuestionText)
                            || quiz.AnswerOptions == null
                            || quiz.AnswerOptions.Count == 0)
                            throw new Exception("Failed to generate valid quiz data from AI.");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Invalid AI JSON: {ex.Message}");
                        j--;
                        continue;
                    }
                    

                    previousQuizText += quiz.QuestionText + ", ";

                    var createdQuiz = await _quizService.CreateQuizAsync(new QuizRequestDto
                    {
                        QuizSetId = quizSetId,
                        QuestionText = quiz.QuestionText,
                        TOEICPart = QuizPartEnums.PART4.ToString(),
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
            return true;
        }
        
        public async Task<bool> GeneratePracticeQuizSetPart5Async(AiGenerateQuizSetRequestDto inputData, Guid quizSetId)
        {
            string previousQuestionText = string.Empty;

            for (int i = 0; i < inputData.QuestionQuantity; i++)
            {
                var prompt = _promptGenerator.GetPart5Prompt(inputData, previousQuestionText);
                var response = await GeminiGenerateContentAsync(prompt);
                AiGenerateQuizResponseDto? quiz;
                try
                {
                    quiz = JsonSerializer.Deserialize<AiGenerateQuizResponseDto>(response);
                    if (quiz == null
                        || string.IsNullOrEmpty(quiz.QuestionText)
                        || quiz.AnswerOptions == null
                        || quiz.AnswerOptions.Count == 0)
                        throw new JsonException("Failed to generate valid quiz data from AI.");
                }
                catch(Exception ex)
                {
                    Console.WriteLine($"Invalid AI JSON: {ex.Message}");
                    i--;
                    continue;
                }

                var createdQuiz = await _quizService.CreateQuizAsync(new QuizRequestDto
                {
                    QuizSetId = quizSetId,
                    QuestionText = quiz.QuestionText,
                    TOEICPart = QuizPartEnums.PART5.ToString(),
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
            return true;
        }

        public async Task<bool> GeneratePracticeQuizSetPart6Async(AiGenerateQuizSetRequestDto inputData, Guid quizSetId)
        {
            string previousPassages = string.Empty;

            for (int i = 0; i < inputData.QuestionQuantity; i += 4)
            {
                var passagePrompt = _promptGenerator.GetPart6PassagePrompt(inputData, previousPassages);
                var passageResponse = await GeminiGenerateContentAsync(passagePrompt);
                AiGenerateQuizResponseDto? passageResult;
                try
                {
                    passageResult = JsonSerializer.Deserialize<AiGenerateQuizResponseDto>(passageResponse);
                    if (passageResult == null || string.IsNullOrEmpty(passageResult.Passage))
                        throw new JsonException("Failed to generate valid passage from AI.");
                }
                catch(Exception ex)
                {
                    Console.WriteLine($"Invalid AI JSON: {ex.Message}");
                    i--;
                    continue;
                }
                
                previousPassages = string.Join("\n", passageResult.Passage);
                var groupItem = await _quizGroupItemService.CreateAsync(new RequestQuizGroupItemDto
                {
                    QuizSetId = quizSetId,
                    Name = $"Group6_{i / 4 + 1}",
                    PassageText = passageResult.Passage
                });

                if(groupItem == null)
                {
                    Console.WriteLine("Failed to create quiz group item.");
                    i -= 4;
                    continue;
                }

                var usedBlanks = string.Empty;

                for (int j = 1; j <= 4 && i + j <= inputData.QuestionQuantity; j++)
                {
                    var quizPrompt = _promptGenerator.GetPart6QuizPrompt(passageResult.Passage, j, usedBlanks);
                    var response = await GeminiGenerateContentAsync(quizPrompt);
                    AiGenerateQuizResponseDto? quiz;
                    try
                    {
                        quiz = JsonSerializer.Deserialize<AiGenerateQuizResponseDto>(response);
                        if (quiz == null
                            || string.IsNullOrEmpty(quiz.QuestionText)
                            || quiz.AnswerOptions == null
                            || quiz.AnswerOptions.Count == 0)
                            throw new JsonException("Failed to generate valid quiz data from AI.");
                    }
                    catch(Exception ex)
                    {
                        Console.WriteLine($"Invalid AI JSON: {ex.Message}");
                        j--;
                        continue;
                    }
                    
                    //create quiz
                    var createdQuiz = await _quizService.CreateQuizAsync(new QuizRequestDto
                    {
                        QuizSetId = quizSetId,
                        QuestionText = j.ToString(),
                        TOEICPart = QuizPartEnums.PART6.ToString(),
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
            return true;
        }

        public async Task<bool> GeneratePracticeQuizSetPart7Async(AiGenerateQuizSetRequestDto inputData, Guid quizSetId)
        {
            string previousPassages = string.Empty;

            for (int i = 0; i < inputData.QuestionQuantity; i += 3)
            {
                var passagePrompt = _promptGenerator.GetPart7PassagePrompt(inputData, previousPassages);
                var passageResponse = await GeminiGenerateContentAsync(passagePrompt);
                AiGenerateQuizResponseDto? passageResult;
                try
                {
                    passageResult = JsonSerializer.Deserialize<AiGenerateQuizResponseDto>(passageResponse);
                    if (passageResult == null || string.IsNullOrEmpty(passageResult.Passage))
                        throw new JsonException("Failed to generate valid passage.");
                }
                catch(Exception ex)
                {
                    Console.WriteLine($"Invalid AI JSON: {ex.Message}");
                    i--;
                    continue;
                }

                previousPassages = passageResult.Passage;

                var groupItem = await _quizGroupItemService.CreateAsync(new RequestQuizGroupItemDto
                {
                    QuizSetId = quizSetId,
                    Name = $"Group7_{i / 3 + 1}",
                    PassageText = passageResult.Passage
                });

                if(groupItem == null)
                {
                    Console.WriteLine("Failed to create quiz group item.");
                    i -= 3;
                    continue;
                }

                string previousQuizText = string.Empty;
                for (int j = 1; j <= 3 && i + j <= inputData.QuestionQuantity; j++)
                {
                    var quizPrompt = _promptGenerator.GetPart7QuizPrompt(passageResult.Passage, previousQuizText);
                    var response = await GeminiGenerateContentAsync(quizPrompt);
                    AiGenerateQuizResponseDto? quiz;
                    try
                    {
                        quiz = JsonSerializer.Deserialize<AiGenerateQuizResponseDto>(response);
                        if (quiz == null
                            || string.IsNullOrEmpty(quiz.QuestionText)
                            || quiz.AnswerOptions == null
                            || quiz.AnswerOptions.Count == 0)
                            throw new JsonException("Failed to generate valid quiz data from AI.");
                    }
                    catch(Exception e)
                    {
                        Console.WriteLine($"Invalid AI JSON: {e.Message}");
                        j--;
                        continue;
                    }


                    previousQuizText += quiz.QuestionText + ", ";

                    var createdQuiz = await _quizService.CreateQuizAsync(new QuizRequestDto
                    {
                        QuizSetId = quizSetId,
                        QuestionText = quiz.QuestionText,
                        TOEICPart = QuizPartEnums.PART7.ToString(),
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

            return true;
        }

        public async Task<PaginationResponseDto<ResponseUserWeakPointDto>> AnalyzeUserMistakesAndAdviseAsync(Guid userId)
        {
            var userMistakes = await _userMistakeService.GetAllByUserIdAsync(userId, new PaginationRequestDto
            {
                Page = 1,
                PageSize = 100
            });
            var existingWeakPoints = string.Empty;
            var existingAdvices = string.Empty;

            foreach (var mistake in userMistakes.Data)
            {
                if (mistake.IsAnalyzed) continue;
                //Trace back to quiz and quiz set
                var quiz = await _quizService.GetQuizByIdAsync(mistake.QuizId);
                if (quiz == null) continue;
                var quizSet = await _quizSetService.GetQuizSetByIdAsync(quiz.QuizSetId);
                if (quizSet == null) continue;
                var answers = await _answerOptionService.GetByQuizIdAsync(quiz.Id);
                if( answers == null || answers.Count() == 0) continue;

                string answersText = string.Empty;
                answersText = string.Join("\n", answers.Select(a => $"{a.OptionLabel}. {a.OptionText} (Correct: {a.IsCorrect})"));

                var userWeakPoints = await _userWeakPointService.GetByUserIdAsync(userId, null!);

                foreach (var wp in userWeakPoints.Data)
                {
                    if (userWeakPoints == null || userWeakPoints.Data.Count() == 0) continue;
                    existingWeakPoints += wp.WeakPoint + "\n";

                    if (!string.IsNullOrEmpty(wp.Advice))
                        existingAdvices += wp.Advice + "\n";
                }

                await _userMistakeService.UpdateAsync(mistake.Id, new RequestUserMistakeDto
                {
                    IsAnalyzed = true
                });

                var prompt = _promptGenerator.GetAnalyzeMistakePrompt(quizSet, quiz, answersText, mistake);
                var response = await GeminiGenerateContentAsync(prompt);

                AiAnalyzeWeakpointResponseDto? analysisResult;
                try
                {
                    analysisResult = JsonSerializer.Deserialize<AiAnalyzeWeakpointResponseDto>(response);
                    if (analysisResult == null || string.IsNullOrEmpty(analysisResult.WeakPoint))
                        throw new JsonException("Failed to generate valid analysis data from AI.");
                }
                catch(Exception ex)
                {
                    Console.WriteLine($"Invalid AI JSON: {ex.Message}");
                    continue;
                }

                if (analysisResult == null
                    || string.IsNullOrEmpty(analysisResult.WeakPoint)
                    || string.IsNullOrEmpty(analysisResult.Advice))
                {
                    Console.WriteLine($"Weak point can't be null");
                    continue;
                }

                if (await _userWeakPointService.IsWeakPointExistedAsync(analysisResult.WeakPoint))
                {
                    Console.WriteLine($"Weak point is existed");
                    continue;
                }

                var newUserWeakPoint = await _userWeakPointService.AddAsync(new RequestUserWeakPointDto
                {
                    UserId = userId,
                    WeakPoint = analysisResult.WeakPoint,
                    Advice = analysisResult.Advice,
                    ToeicPart = quiz.TOEICPart,
                    DifficultyLevel = quizSet.DifficultyLevel,
                    IsDone = false
                });
            }

            return await _userWeakPointService.GetByUserIdAsync(userId, null!);
        }

        public async Task<QuizSetResponseDto> GenerateFixWeakPointQuizSetAsync(Guid userId)
        {
            /*//Get all user weak points that are not done yet
            var userWeakPoints = await _userWeakPointService.GetByUserIdAsync(userId, null!);
            string[] parts = [];
            Dictionary<string, ResponseUserWeakPointDto> weakPointsInTheSamePart = new();//Key: Part, Value: WeakPoint
            foreach (var wp in userWeakPoints.Data)
            {
                if(wp.IsDone) continue;
                //Take all the weak points that have the same Part
                weakPointsInTheSamePart.Add(wp.ToeicPart, wp);

                if (!parts.Contains(wp.ToeicPart))
                {
                    parts = parts.Append(wp.ToeicPart).ToArray();
                }

                await _userWeakPointService.UpdateAsync(wp.Id, new RequestUserWeakPointDto
                {
                    UserId = wp.UserId,
                    WeakPoint = wp.WeakPoint,
                    Advice = wp.Advice,
                    ToeicPart = wp.ToeicPart,
                    DifficultyLevel = wp.DifficultyLevel,
                    IsDone = true,
                    CompleteAt = DateTime.UtcNow
                });

            }

            //Take each weak points in the same part to generate a quiz set about 10 quizzes
            foreach(var part in parts)
            {
                foreach(var wp in weakPointsInTheSamePart)
                {
                    //Generate quiz here
                }
            }*/
            throw new Exception("Not implemented yet.");
        }
    }
}
