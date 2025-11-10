using BusinessLogic.DTOs.AiDtos;
using Microsoft.AspNetCore.Components.Forms;

namespace BusinessLogic.Helpers
{
    public enum VoiceRoles
    {
        Male,
        Female,
        Narrator
    }
    public class PromptGenerateQuizSetHelper
    {
        public string GenerateQuizSetPart1Prompt(AiGenerateQuizSetRequestDto inputData, string previousImageDescription, string previousQuestionText)
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
            return prompt;
        }

        public string GenerateQuizSetPart2Prompt()
        {
            var prompt = $@"";
            return prompt;
        }

        public (string, string) GenerateQuizSetPart3Prompt()
        {
            var audioPrompt = $@"";
            var quizPrompt = $@"";

            return (audioPrompt, quizPrompt);
        }
        public (string, string) GenerateQuizSetPart4Prompt()
        {
            var audioPrompt = $@"";
            var quizPrompt = $@"";

            return (audioPrompt, quizPrompt);
        }
        public string GenerateQuizSetPart5Prompt()
        {
            var prompt = $@"";
            return prompt;
        }

    }
}
