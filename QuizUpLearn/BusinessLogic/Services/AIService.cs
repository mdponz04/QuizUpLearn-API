using BusinessLogic.DTOs;
using BusinessLogic.DTOs.AiDtos;
using BusinessLogic.DTOs.QuizDtos;
using BusinessLogic.DTOs.QuizGroupItemDtos;
using BusinessLogic.DTOs.UserMistakeDtos;
using BusinessLogic.DTOs.UserWeakPointDtos;
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
        private readonly IQuizService _quizService;
        private readonly IAnswerOptionService _answerOptionService;
        private readonly IUploadService _uploadService;
        private readonly ILogger<AIService> _logger;
        private readonly IQuizGroupItemService _quizGroupItemService;
        private readonly IUserMistakeService _userMistakeService;
        private readonly IUserWeakPointService _userWeakPointService;
        private readonly IQuizQuizSetService _quizQuizSetService;
        private readonly IGrammarService _grammarService;
        private readonly IVocabularyService _vocabularyService;
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
        public AIService(HttpClient httpClient, IConfiguration configuration, IQuizService quizService, IAnswerOptionService answerOptionService, IUploadService uploadService, ILogger<AIService> logger, IQuizGroupItemService quizGroupItemService, IUserMistakeService userMistakeService, IUserWeakPointService userWeakPointService, IQuizQuizSetService quizQuizSetService, IGrammarService grammarService, IVocabularyService vocabularyService)
        {
            _httpClient = httpClient;
            _quizService = quizService;
            _answerOptionService = answerOptionService;
            _uploadService = uploadService;
            _logger = logger;
            _quizGroupItemService = quizGroupItemService;
            _userMistakeService = userMistakeService;
            _userWeakPointService = userWeakPointService;
            _quizQuizSetService = quizQuizSetService;
            _grammarService = grammarService;
            _vocabularyService = vocabularyService;

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

            var body = new
            {
                model = "tngtech/deepseek-r1t2-chimera:free",
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
            
            using var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = content
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _openRouterApiKey);

            using var response = await _httpClient.SendAsync(request);
            var result = await response.Content.ReadAsStringAsync();

            using var doc = JsonDocument.Parse(result);
            if (!doc.RootElement.TryGetProperty("choices", out var choices))
                return json;

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

        private async Task<byte[]> GenerateConversationAudioAsync(List<ConversationLineDto> conversation)
        {
            using var combinedStream = new MemoryStream();
            var silenceBytes = new byte[13230];

            foreach (var line in conversation)
            {
                var lineAudioBytes = await GenerateAudioAsync(line.Text, line.Role);

                await combinedStream.WriteAsync(lineAudioBytes, 0, lineAudioBytes.Length);

                if (line != conversation.Last())
                    await combinedStream.WriteAsync(silenceBytes, 0, silenceBytes.Length);
            }

            return combinedStream.ToArray();
        }

        private async Task<AiGenerateResponseDto> GenerateWithRetryAsync(List<string> purposes, string prompt)
        {
            int retryCount = 0;
            while (true)
            {
                if (retryCount >= 3)
                {
                    throw new Exception("Maximum retry attempts reached for AI generation.");
                }

                var response = await GeminiGenerateContentAsync(prompt);
                try
                {
                    var quizResult = JsonSerializer.Deserialize<AiGenerateResponseDto>(response);

                    if (quizResult == null) throw new Exception("Invalid json");

                    if (purposes.Contains(GenerationPurpose.QUIZ))
                    {
                        if (string.IsNullOrWhiteSpace(quizResult.QuestionText) ||
                                quizResult.AnswerOptions == null ||
                                quizResult.AnswerOptions.Count == 0)
                            throw new JsonException("Invalid quiz");
                    }
                    if (purposes.Contains(GenerationPurpose.AUDIO))
                    {
                        if (string.IsNullOrWhiteSpace(quizResult.AudioScript))
                            throw new JsonException("Invalid audio script");
                    }
                    if (purposes.Contains(GenerationPurpose.IMAGE))
                    {
                        if (string.IsNullOrWhiteSpace(quizResult.ImageDescription))
                            throw new JsonException("Invalid image description");
                    }
                    if (purposes.Contains(GenerationPurpose.PASSAGE))
                    {
                        if (string.IsNullOrWhiteSpace(quizResult.Passage))
                            throw new JsonException("Invalid passage");
                    }
                    if (purposes.Contains(GenerationPurpose.CONVERSATION_AUDIO))
                    {
                        if (quizResult.AudioScripts == null || quizResult.AudioScripts.Count == 0)
                            throw new JsonException("Invalid conversation audio scripts");
                    }
                    if (purposes.Contains(GenerationPurpose.ANALYZE_MISTAKE))
                    {
                        if (string.IsNullOrEmpty(quizResult.WeakPoint) || string.IsNullOrEmpty(quizResult.Advice))
                            throw new JsonException("Invalid analyze mistake result");
                    }
                    return quizResult;
                }
                catch
                {
                    retryCount++;
                    Console.WriteLine("AI JSON invalid. Retrying...");
                }
            }
        }

        private async Task<string> CreateAudioAsync(string? script, List<ConversationLineDto>? AudioScripts, Guid userId, int part, VoiceRoles? voiceRole = null)
        {
            byte[] audioBytes = [];

            if (script == null && AudioScripts != null)
            {
                audioBytes = await GenerateConversationAudioAsync(AudioScripts);
            }
            if (script != null && AudioScripts == null)
            {
                audioBytes = await GenerateAudioAsync(script, voiceRole ?? VoiceRoles.Narrator);
            }

            var file = await _uploadService.ConvertByteArrayToIFormFile(audioBytes, $"audio-Part{part}-{userId}-{DateTime.UtcNow}.mp3", "audio/mpeg");
            var result = await _uploadService.UploadAsync(file);
            return result.Url;
        }

        private async Task<string> CreateImageAsync(string description, Guid userId, int part)
        {
            var imageBytes = await GenerateImageAsync(description);
            var file = await _uploadService.ConvertByteArrayToIFormFile(imageBytes, $"image-Part{part}-{userId}-{DateTime.UtcNow}.png", "image/png");
            var result = await _uploadService.UploadAsync(file);
            return result.Url;
        }

        private async Task<QuizResponseDto?> CreateQuizWithOptionsAsync(Guid grammarId, Guid vocabId , string part, string questionText, List<AiGenerateAnswerOptionResponseDto> options, bool isAssignOptionText = true, Guid? groupItemId = null, string? audioUrl = null, string? imageUrl = null)
        {
            QuizDifficultyLevelDetermine quizDetermineDifficulty = new();
            var grammar = await _grammarService.GetByIdAsync(grammarId);
            var vocabulary = await _vocabularyService.GetByIdAsync(vocabId);
            if(grammar == null || vocabulary == null)
                throw new ArgumentException("Grammar or Vocabulary not found");

            var quiz = await _quizService.CreateQuizAsync(new QuizRequestDto
            {
                TOEICPart = part,
                QuestionText = questionText,
                QuizGroupItemId = groupItemId,
                AudioURL = audioUrl,
                ImageURL = imageUrl,
                GrammarId = grammarId,
                VocabularyId = vocabId,
                IsAIGenerated = true,
                DifficultyLevel = await quizDetermineDifficulty.DetermineQuizDifficultyLevel(grammar, vocabulary)
            });

            foreach (var opt in options)
            {
                if (!isAssignOptionText)
                {
                    opt.OptionText = string.Empty;
                }
                await _answerOptionService.CreateAsync(new RequestAnswerOptionDto
                {
                    OptionLabel = opt.OptionLabel,
                    OptionText = opt.OptionText,
                    IsCorrect = opt.IsCorrect,
                    QuizId = quiz.Id
                });
            }

            /*var isValid = await ValidateQuizAsync(quiz.Id);

            if (!isValid) 
            {
                return null;
            }*/

            return quiz;
        }

        private async Task<ResponseQuizGroupItemDto?> CreateQuizGroupItem(string name, string? audioUrl = null, string? audioScript = null, string? imageUrl = null, string? imageDescription = null, string? passageText = null)
        {
            var groupItem = await _quizGroupItemService.CreateAsync(new RequestQuizGroupItemDto
            {
                Name = name,
                AudioUrl = audioUrl,
                AudioScript = audioScript,
                ImageUrl = imageUrl,
                ImageDescription = imageDescription,
                PassageText = passageText
            });
            return groupItem;
        }

        private async Task<bool> ValidateQuizAsync(Guid quizId)
        {
            if (quizId == Guid.Empty)
                return false;

            bool isValid = false;

            int retryCount = 0;
            var groupPassage = string.Empty;
            var groupAudioScript = string.Empty;
            var groupImageDescription = string.Empty;

            var quiz = await _quizService.GetQuizByIdAsync(quizId);

            var keyword = quiz.Vocabulary.KeyWord;
            var grammar = quiz.Grammar.Name;
            //retry validate if json that ai return is invalid
            while(retryCount < 3)
            {
                try
                {
                    if (quiz.QuizGroupItemId != null)
                    {
                        var groupItems = await _quizGroupItemService.GetByIdAsync(quiz.QuizGroupItemId.Value);
                        if (groupItems != null)
                        {
                            groupPassage = groupItems.PassageText ?? string.Empty;
                            groupAudioScript = groupItems.AudioScript ?? string.Empty;
                            groupImageDescription = groupItems.ImageDescription ?? string.Empty;
                        }
                    }
                    var options = await _answerOptionService.GetByQuizIdAsync(quiz.Id);

                    var validationPrompt = _promptGenerator.GetValidationPromptAsync(quiz, quiz.AnswerOptions, groupPassage, groupAudioScript, groupImageDescription, grammar, keyword);
                    var response = await OpenRouterGenerateContentAsync(validationPrompt);

                    var validation = JsonSerializer.Deserialize<AiValidationResponseDto>(response ?? string.Empty);

                    if (validation == null)
                    {
                        Console.WriteLine($"Quiz {quiz.Id}: Validation failed (null response).");
                        retryCount++;
                    }

                    if (!validation.IsValid)
                    {
                        await _quizService.HardDeleteQuizAsync(quiz.Id);
                        isValid = false;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Quiz {quiz.Id}: Validation error - {ex.Message}");
                    retryCount++;
                }
            }

            return isValid;
        }
        
        private async Task<(Guid quizGroupItemId, Guid? singleQuizId)> GeneratePracticeQuizSetPart1Async(AiGenerateQuizRequestDto inputData)
        {
            Guid quizGroupItemId = Guid.Empty;
            var grammar = await _grammarService.GetByIdAsync(inputData.GrammarId);
            var vocabulary = await _vocabularyService.GetByIdAsync(inputData.VocabularyId);
            
            if (grammar == null || vocabulary == null)
                throw new ArgumentException("Grammar or Vocabulary not found");

            string previousImageDescription = string.Empty;
            string previousQuestionText = string.Empty;

            for (int i = 0; i < inputData.QuestionQuantity; i++)
            {
                var prompt = _promptGenerator.GetQuizSetPart1Prompt(inputData, previousImageDescription, previousQuestionText, vocabulary.KeyWord, grammar.Name);
                List<string> purposes = new() { GenerationPurpose.QUIZ, GenerationPurpose.IMAGE };
                AiGenerateResponseDto quiz = await GenerateWithRetryAsync(purposes, prompt);

                var audioScript = string.Join(Environment.NewLine, quiz.AnswerOptions.Select(o => $"{o.OptionLabel}. {o.OptionText}"));
                string audioUrl = await CreateAudioAsync(audioScript, null, inputData.CreatorId!.Value, 1);
                string imageUrl = await CreateImageAsync(quiz.ImageDescription!, inputData.CreatorId!.Value, 1);

                var groupItem = await CreateQuizGroupItem($"Single quiz {QuizPartEnum.PART1.ToString()}", audioUrl, audioScript, imageUrl, quiz.ImageDescription);
                
                if (groupItem == null)
                {
                    Console.WriteLine("Failed to create quiz group item.");
                    i--;
                    continue;
                }

                quizGroupItemId = groupItem.Id;

                var createdQuiz = await CreateQuizWithOptionsAsync(inputData.GrammarId
                    , inputData.VocabularyId
                    , QuizPartEnum.PART1.ToString()
                    , ""
                    , quiz.AnswerOptions
                    , false
                    , groupItem.Id
                    , audioUrl
                    , imageUrl);

                previousImageDescription += quiz.ImageDescription + "\n";
                previousQuestionText += quiz.QuestionText + ", ";
            }

            return (quizGroupItemId, null);
        }
        
        private async Task<(Guid quizGroupItemId, Guid? singleQuizId)> GeneratePracticeQuizSetPart2Async(AiGenerateQuizRequestDto inputData)
        {
            Guid quizGroupItemId = Guid.Empty;
            var grammar = await _grammarService.GetByIdAsync(inputData.GrammarId);
            var vocabulary = await _vocabularyService.GetByIdAsync(inputData.VocabularyId);
            
            if (grammar == null || vocabulary == null)
                throw new ArgumentException("Grammar or Vocabulary not found");

            string previousQuestionText = string.Empty;
            for (int i = 0; i < inputData.QuestionQuantity; i++)
            {
                var prompt = _promptGenerator.GetQuizSetPart2Prompt(inputData, previousQuestionText, vocabulary.KeyWord, grammar.Name);
                List<string> purposes = new() { GenerationPurpose.QUIZ };
                AiGenerateResponseDto quiz = await GenerateWithRetryAsync(purposes, prompt);
                
                var audioScript = "Question: " + quiz.QuestionText + ".\n" + string.Join(Environment.NewLine, quiz.AnswerOptions.Select(o => $"{o.OptionLabel}. {o.OptionText}"));
                var audioUrl = await CreateAudioAsync(audioScript, null, inputData.CreatorId!.Value, 2);

                var groupItem = await CreateQuizGroupItem($"Single quiz {QuizPartEnum.PART2.ToString()}", audioUrl, audioScript);
                if (groupItem == null)
                {
                    Console.WriteLine("Failed to create quiz group item.");
                    i--;
                    continue;
                }
                quizGroupItemId = groupItem.Id;
                var createdQuiz = await CreateQuizWithOptionsAsync(inputData.GrammarId
                    , inputData.VocabularyId
                    , QuizPartEnum.PART2.ToString()
                    , ""
                    , quiz.AnswerOptions
                    , false
                    , groupItem.Id
                    , audioUrl);

                previousQuestionText += quiz.QuestionText + ", ";
            }
            return (quizGroupItemId, null);
        }

        private async Task<(Guid quizGroupItemId, Guid? singleQuizId)> GeneratePracticeQuizSetPart3Async(AiGenerateQuizRequestDto inputData)
        {
            Guid quizGroupItemId = Guid.Empty;
            var grammar = await _grammarService.GetByIdAsync(inputData.GrammarId);
            var vocabulary = await _vocabularyService.GetByIdAsync(inputData.VocabularyId);

            if (grammar == null || vocabulary == null)
                throw new ArgumentException("Grammar or Vocabulary not found");

            var previousAudioScript = string.Empty;

            for (int i = 0; i < inputData.QuestionQuantity; i += 3)
            {
                var audioPrompt = _promptGenerator.GetPart3AudioPrompt(inputData, previousAudioScript, vocabulary.KeyWord, grammar.Name);
                List<string> purposes = new() { GenerationPurpose.CONVERSATION_AUDIO };

                AiGenerateResponseDto conversationScripts = await GenerateWithRetryAsync(purposes, audioPrompt);

                string audioConversationScripts = string.Join(Environment.NewLine, conversationScripts.AudioScripts.Select(auScript => $"{auScript.Role}: {auScript.Text}"));
                previousAudioScript = string.Join("\n", audioConversationScripts);
                var audioUrl = await CreateAudioAsync(null, conversationScripts.AudioScripts, inputData.CreatorId!.Value, 3);

                var groupItem = await CreateQuizGroupItem($"Group3_{i / 3 + 1}", audioUrl, audioConversationScripts);
                if(groupItem == null)
                {
                    Console.WriteLine("Failed to create quiz group item.");
                    i -= 3;
                    continue;
                }
                quizGroupItemId = groupItem.Id;
                string previousQuizText = string.Empty;

                for (int j = 0; j < 3 && i + j < inputData.QuestionQuantity; j++)
                {
                    var quizPrompt = _promptGenerator.GetPart3QuizPrompt(
                        audioConversationScripts, 
                        previousQuizText, 
                        vocabulary.KeyWord, 
                        grammar.Name);
                    List<string> quizPurpose = new() { GenerationPurpose.QUIZ };
                    AiGenerateResponseDto quiz = await GenerateWithRetryAsync(quizPurpose, quizPrompt);

                    previousQuizText += quiz.QuestionText + ", ";
                    var createdQuiz = await CreateQuizWithOptionsAsync(grammar.Id
                        , vocabulary.Id
                        , QuizPartEnum.PART3.ToString()
                        , quiz.QuestionText
                        , quiz.AnswerOptions
                        , groupItemId: groupItem.Id);
                }
            }
            return (quizGroupItemId, null);
        }

        private async Task<(Guid quizGroupItemId, Guid? singleQuizId)> GeneratePracticeQuizSetPart4Async(AiGenerateQuizRequestDto inputData)
        {
            Guid quizGroupItemId = Guid.Empty;
            var grammar = await _grammarService.GetByIdAsync(inputData.GrammarId);
            var vocabulary = await _vocabularyService.GetByIdAsync(inputData.VocabularyId);

            if (grammar == null || vocabulary == null)
                throw new ArgumentException("Grammar or Vocabulary not found");

            string previousAudioScript = string.Empty;
            
            for (int i = 0; i < inputData.QuestionQuantity; i += 3)
            {
                var audioPrompt = _promptGenerator.GetPart4AudioPrompt(inputData, previousAudioScript, vocabulary.KeyWord, grammar.Name);
                List<string> purposes = new() { GenerationPurpose.AUDIO };

                AiGenerateResponseDto audio = await GenerateWithRetryAsync(purposes, audioPrompt);
                
                previousAudioScript = string.Join("\n", audio.AudioScript);

                var audioUrl = await CreateAudioAsync(audio.AudioScript!, null, inputData.CreatorId!.Value, 4);

                var groupItem = await CreateQuizGroupItem($"Group4_{i / 3 + 1}", audioUrl, audio.AudioScript);
                
                if(groupItem == null)
                {
                    Console.WriteLine("Failed to create quiz group item.");
                    i -= 3;
                    continue;
                }
                quizGroupItemId = groupItem.Id;
                string previousQuizText = string.Empty;
                for (int j = 0; j < 3 && i + j < inputData.QuestionQuantity; j++)
                {
                    var quizPrompt = _promptGenerator.GetPart4QuizPrompt(
                        audio.AudioScript, 
                        previousQuizText, 
                        vocabulary.KeyWord, 
                        grammar.Name);
                    List<string> quizPurposes = new() { GenerationPurpose.QUIZ };

                    AiGenerateResponseDto quiz = await GenerateWithRetryAsync(quizPurposes, quizPrompt);

                    previousQuizText += quiz.QuestionText + ", ";

                    var createdQuiz = await CreateQuizWithOptionsAsync(grammar.Id
                        , vocabulary.Id
                        , QuizPartEnum.PART4.ToString()
                        , quiz.QuestionText
                        , quiz.AnswerOptions
                        , groupItemId: groupItem.Id);
                }
            }
            return (quizGroupItemId, null);
        }

        private async Task<(Guid quizGroupItemId, Guid? singleQuizId)> GeneratePracticeQuizSetPart5Async(AiGenerateQuizRequestDto inputData)
        {
            Guid singleQuizId = Guid.Empty;
            var grammar = await _grammarService.GetByIdAsync(inputData.GrammarId);
            var vocabulary = await _vocabularyService.GetByIdAsync(inputData.VocabularyId);

            if (grammar == null || vocabulary == null)
                throw new ArgumentException("Grammar or Vocabulary not found");

            string previousQuestionText = string.Empty;

            for (int i = 0; i < inputData.QuestionQuantity; i++)
            {
                var prompt = _promptGenerator.GetPart5Prompt(inputData, previousQuestionText, vocabulary.KeyWord, grammar.Name);
                List<string> purposes = new() { GenerationPurpose.QUIZ };

                AiGenerateResponseDto quiz = await GenerateWithRetryAsync(purposes, prompt);

                var createdQuiz = await CreateQuizWithOptionsAsync(grammar.Id
                    , vocabulary.Id
                    , QuizPartEnum.PART5.ToString()
                    , quiz.QuestionText
                    , quiz.AnswerOptions);

                previousQuestionText += quiz.QuestionText + ", ";

                singleQuizId = createdQuiz.Id;
            }
            return (Guid.Empty, singleQuizId);
        }

        private async Task<(Guid quizGroupItemId, Guid? singleQuizId)> GeneratePracticeQuizSetPart6Async(AiGenerateQuizRequestDto inputData)
        {
            Guid quizGroupItemId = Guid.Empty;
            var grammar = await _grammarService.GetByIdAsync(inputData.GrammarId);
            var vocabulary = await _vocabularyService.GetByIdAsync(inputData.VocabularyId);

            if (grammar == null || vocabulary == null)
                throw new ArgumentException("Grammar or Vocabulary not found");

            string previousPassages = string.Empty;

            for (int i = 0; i < inputData.QuestionQuantity; i += 4)
            {
                var passagePrompt = _promptGenerator.GetPart6PassagePrompt(inputData, previousPassages, vocabulary.KeyWord, grammar.Name);
                List<string> purposes = new() { GenerationPurpose.PASSAGE };

                AiGenerateResponseDto passageResult = await GenerateWithRetryAsync(purposes, passagePrompt);
                
                previousPassages = string.Join("\n", passageResult.Passage);

                var groupItem = await CreateQuizGroupItem($"Group6_{i / 4 + 1}", passageText: passageResult.Passage);

                if(groupItem == null)
                {
                    Console.WriteLine("Failed to create quiz group item.");
                    i -= 4;
                    continue;
                }
                quizGroupItemId = groupItem.Id;
                var usedBlanks = string.Empty;

                for (int j = 1; j <= 4 && i + j <= inputData.QuestionQuantity; j++)
                {
                    var quizPrompt = _promptGenerator.GetPart6QuizPrompt(
                        passageResult.Passage, 
                        j, 
                        usedBlanks,
                        vocabulary.KeyWord,
                        grammar.Name);
                    List<string> quizPurposes = new() { GenerationPurpose.QUIZ };
                    
                    AiGenerateResponseDto quiz = await GenerateWithRetryAsync(quizPurposes, quizPrompt);
                    
                    var createdQuiz = await _quizService.CreateQuizAsync(new QuizRequestDto
                    {
                        QuestionText = j.ToString(),
                        TOEICPart = QuizPartEnum.PART6.ToString(),
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

                        if (item.IsCorrect)
                        {
                            usedBlanks += $"{j}." + item.OptionText + ", ";
                        }
                    }
                    passageResult.Passage = quiz.QuestionText;
                }
                //Update the last passage with all blanks
                await _quizGroupItemService.UpdateAsync(groupItem.Id, new RequestQuizGroupItemDto
                {
                    PassageText = passageResult.Passage
                });

            }
            return (quizGroupItemId, null);
        }

        private async Task<(Guid quizGroupItemId, Guid? singleQuizId)> GeneratePracticeQuizSetPart7Async(AiGenerateQuizRequestDto inputData)
        {
            Guid quizGroupItemId = Guid.Empty;
            var grammar = await _grammarService.GetByIdAsync(inputData.GrammarId);
            var vocabulary = await _vocabularyService.GetByIdAsync(inputData.VocabularyId);

            if (grammar == null || vocabulary == null)
                throw new ArgumentException("Grammar or Vocabulary not found");

            string previousPassages = string.Empty;

            for (int i = 0; i < inputData.QuestionQuantity; i += inputData.QuestionQuantity)
            {
                var passagePrompt = _promptGenerator.GetPart7PassagePrompt(inputData, previousPassages, vocabulary.KeyWord, grammar.Name);
                List<string> purposes = new() { GenerationPurpose.PASSAGE };
                
                AiGenerateResponseDto passageResult = await GenerateWithRetryAsync(purposes, passagePrompt);

                previousPassages = passageResult.Passage;

                var groupItem = await CreateQuizGroupItem($"Group7_{i / 3 + 1}", passageText: passageResult.Passage);

                if(groupItem == null)
                {
                    Console.WriteLine("Failed to create quiz group item.");
                    i -= inputData.QuestionQuantity;
                    continue;
                }
                quizGroupItemId = groupItem.Id;
                string previousQuizText = string.Empty;
                for (int j = 1; j <= inputData.QuestionQuantity && i + j <= inputData.QuestionQuantity; j++)
                {
                    var quizPrompt = _promptGenerator.GetPart7QuizPrompt(
                        passageResult.Passage, 
                        previousQuizText,
                        vocabulary.KeyWord,
                        grammar.Name);
                    List<string> quizPurposes = new() { GenerationPurpose.QUIZ };

                    AiGenerateResponseDto quiz = await GenerateWithRetryAsync(quizPurposes, quizPrompt);

                    previousQuizText += quiz.QuestionText + ", ";

                    var createdQuiz = await CreateQuizWithOptionsAsync(grammar.Id
                        , vocabulary.Id
                        , QuizPartEnum.PART7.ToString()
                        , quiz.QuestionText
                        , quiz.AnswerOptions
                        , groupItemId: groupItem.Id);
                }
            }

            return (quizGroupItemId, null);
        }
        
        public async Task AnalyzeUserMistakesAndAdviseAsync(Guid userId)
        {
            var userMistakes = await _userMistakeService.GetAllByUserIdAsync(userId);

            for (int i = 1; i <= 7; i++)
            {
                var listDataByPart = userMistakes
                    .Where(m => m.Quiz.TOEICPart == $"PART{i}")
                    .ToList();

                if(listDataByPart.Count() == 0) continue;

                string quizzesPrompt = string.Empty;

                var newUserWeakPoint = await _userWeakPointService.AddAsync(new RequestUserWeakPointDto
                {
                    UserId = userId,
                    WeakPoint = string.Empty,
                    Advice = string.Empty,
                    ToeicPart = $"PART{i}"
                });

                for (int j = 1; j <= listDataByPart.Count(); j++)
                {
                    var mistake = listDataByPart[j - 1];

                    var quiz = await _quizService.GetQuizByIdAsync(mistake.QuizId);
                    if (quiz == null) continue;

                    if (quiz.QuizGroupItemId != null)
                    {
                        var quizGroupItem = await _quizGroupItemService.GetByIdAsync(quiz.QuizGroupItemId.Value);
                    }

                    var answers = await _answerOptionService.GetByQuizIdAsync(quiz.Id);
                    if (answers == null || answers.Count() == 0) continue;

                    var userWeakPoints = await _userWeakPointService.GetByUserIdAsync(userId, null!);

                    string answersText = string.Empty;
                    answersText = string.Join("\n", answers.Select(a => $"{a.OptionLabel}. {a.OptionText} (IsCorrect: {a.IsCorrect})"));

                    var quizPrompt = _promptGenerator.GetAnalyzeMistakeQuizPrompt(quiz, answersText, mistake);

                    quizzesPrompt += quizPrompt + "\n";

                    await _userMistakeService.UpdateAsync(mistake.Id, new RequestUserMistakeDto
                    {
                        UserWeakPointId = newUserWeakPoint!.Id,
                        IsAnalyzed = true
                    });
                }
                
                string prompt = quizzesPrompt + _promptGenerator.GetAnalyzeMistakeGeneratePrompt();
                List<string> purposes = new() { GenerationPurpose.ANALYZE_MISTAKE };
                AiGenerateResponseDto? analysisResult = await GenerateWithRetryAsync(purposes, prompt);

                if (await _userWeakPointService.IsWeakPointExistedAsync(analysisResult.WeakPoint, userId))
                {
                    foreach (var item in listDataByPart)
                    {
                        await _userMistakeService.DeleteAsync(item.Id);
                    }
                    
                    continue;
                }

                await _userWeakPointService.UpdateAsync(newUserWeakPoint!.Id, new RequestUserWeakPointDto
                {
                    WeakPoint = analysisResult.WeakPoint,
                    Advice = analysisResult.Advice,
                    ToeicPart = $"PART{i}"
                });
            }
        }

        public async Task<(Guid quizGroupItemId, Guid? singleQuizId)> GeneratePracticeQuizzesAsync(AiGenerateQuizRequestDto inputData)
        {
            (Guid quizGroupItemId, Guid? singleQuizId) result = (Guid.Empty, null);
            var vocab = await _vocabularyService.GetByIdAsync(inputData.VocabularyId);
            try
            {
                switch (vocab.ToeicPart)
                {
                    case "1":
                        inputData.QuestionQuantity = 1;
                        result = await GeneratePracticeQuizSetPart1Async(inputData);
                        break;
                    case "2":
                        inputData.QuestionQuantity = 1;
                        result = await GeneratePracticeQuizSetPart2Async(inputData);
                        break;
                    case "3":
                        inputData.QuestionQuantity = 3;
                        result = await GeneratePracticeQuizSetPart3Async(inputData);
                        break;
                    case "4":
                        inputData.QuestionQuantity = 3;
                        result = await GeneratePracticeQuizSetPart4Async(inputData);
                        break;
                    case "5":
                        inputData.QuestionQuantity = 1;
                        result = await GeneratePracticeQuizSetPart5Async(inputData);
                        break;
                    case "6":
                        inputData.QuestionQuantity = 4;
                        result = await GeneratePracticeQuizSetPart6Async(inputData);
                        break;
                    case "7":
                        var rnd = new Random();
                        inputData.QuestionQuantity = rnd.Next(2, 5);
                        result = await GeneratePracticeQuizSetPart7Async(inputData);
                        break;
                    default:
                        throw new ArgumentException("Invalid TOEIC part specified in vocabulary.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error generating practice quizzes: {ex.Message}");
                return (Guid.Empty, null);
            }

            return result;
        }
    }
}
