using BusinessLogic.DTOs;
using BusinessLogic.DTOs.AiDtos;
using BusinessLogic.DTOs.QuizDtos;
using BusinessLogic.DTOs.QuizSetDtos;
using BusinessLogic.DTOs.UserMistakeDtos;

namespace BusinessLogic.Helpers
{
    public enum VoiceRoles
    {
        Male,
        Female,
        Narrator
    }
    public class GenerationPurpose
    {
        public const string QUIZ= "QuizGeneration";
        public const string AUDIO = "AudioGeneration";
        public const string IMAGE = "ImageGeneration";
        public const string PASSAGE = "PassageGeneration";
        public const string CONVERSATION_AUDIO = "ConversationAudioGeneration";
    }
    public class PromptGenerateQuizSetHelper
    {
        public PromptGenerateQuizSetHelper()
        {
        }

        public string GetValidationPromptAsync(QuizSetResponseDto quizSet, QuizResponseDto quiz, List<ResponseAnswerOptionDto> options, string groupPassage, string groupAudioScript, string groupImageDescription)
        {
            return $@"
You are an expert TOEIC test validator.
Review this quiz for correctness and clarity.

### Quiz Set Context:
TOEIC practice quiz titled: '{quizSet.Title}'.
Description: {quizSet.Description}, 
Toeic part: {quiz.TOEICPart},
suitable for learners with TOEIC scores around {quizSet.DifficultyLevel}.
suitable for learners with TOEIC scores around {quizSet.DifficultyLevel}.

### Quiz to Validate
Additional info(if any):
- Passage: {groupPassage}

- Audio script: {groupAudioScript}

- Image description: {groupImageDescription}

Question: {quiz.QuestionText}

Options:
{string.Join("\n", options.Select(o => $"{o.OptionLabel}. {o.OptionText} (Correct: {o.IsCorrect})"))}

Check the criteria and :
1. The question is grammatically correct and meaningful.
2. There is ONE or more correct answer.
3. The correct answer makes sense in context.
4. Not duplicating or very similar options.
5. If it is TOEIC part 2, question text and option text will be null because of that all you need to check the audio script, inside that audio script it will have the question and the answer options.
6. No need suggestion improvement, only validate correctness, explain shortly at Feedback field if the question is invalid otherwise return feedback with empty string.

Return only these 2 fields as JSON structure:
{{ 
    ""IsValid"": true/false,
    ""Feedback"": "".....""
}}
";
        }
        public string GetQuizSetPart1Prompt(AiGenerateQuizSetRequestDto inputData, string previousImageDescription, string previousQuestionText)
        {
            return $@"
Topic: '{inputData.Topic}'.
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
}}
";
        }
        public string GetQuizSetPart2Prompt(AiGenerateQuizSetRequestDto inputData, string previousQuestionText)
        {
            return $@"
Topic: '{inputData.Topic}'.
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
}}
";
        }
        public string GetPart3AudioPrompt(AiGenerateQuizSetRequestDto inputData, string previousAudioScript)
        {
            return $@"
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
}}
";
        }
        public string GetPart3QuizPrompt(string audioScript, string previousQuizText)
        {
            return $@"
Based on this audio conversation script: {audioScript}

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
}}
";
        }
        public string GetPart4AudioPrompt(AiGenerateQuizSetRequestDto inputData, string previousAudioScript)
        {
            return $@"
Generate a TOEIC audio topic: '{inputData.Topic}'.
Description: Focus on TOEIC Part 4, 
suitable for learners with TOEIC scores around {inputData.Difficulty}.

Avoid the previous audio script(if it is not null): {previousAudioScript}

Generate ONE audio script that contain a short monologue (announcement/speech) matches this theme.

Only return in this structure:
{{
  ""AudioScript"": ""...""
}}
";
        }
        public string GetPart4QuizPrompt(string audioScript, string previousQuizText)
        {
            return $@"
Based on this audio script: {audioScript}
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
}}
";
        }
        public string GetPart5Prompt(AiGenerateQuizSetRequestDto inputData, string previousQuestionText)
        {
            return $@"
Topic: '{inputData.Topic}'.
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
}}
";
        }
        public string GetPart6PassagePrompt(AiGenerateQuizSetRequestDto inputData, string previousPassages)
        {
            return $@"
Generate a TOEIC passage: 
- Title: {inputData.Topic}.
- Description: Focus on TOEIC Part 6, suitable for learners with TOEIC scores around {inputData.Difficulty}.

Avoid the previous passage(if it is not null): {previousPassages}

Generate ONE passage match the theme - generate fully passage without any blanks (at least 75 words with 6 sentences).

Only return in this structure:
{{
  ""Passage"": ""..."",
}}
";
        }
        public string GetPart6QuizPrompt(string passage, int blankNumber, string usedBlanks)
        {
            return $@"
Based on this passage: {passage}
Replace the {blankNumber}th important word with a blank marked as ({blankNumber}).

Avoid using these previous blanks(if any): [{string.Join(", ", usedBlanks)}]

Return the question text with ({blankNumber}) and 4 answer options (1 correct, 3 wrong). Return the modified passage as 'QuestionText'

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
}}
";
        }
        public string GetPart7PassagePrompt(AiGenerateQuizSetRequestDto inputData, string previousPassages)
        {
            return $@"
Generate a TOEIC Part 7 reading passage about '{inputData.Topic}' 
for learners with TOEIC score around {inputData.Difficulty}.
Avoid repeating previous passages: {previousPassages}.
Length: around 120-150 words.

Return only JSON:
{{
  ""Passage"": ""...""
}}
";
        }
        public string GetPart7QuizPrompt(string passage, string previousQuizText)
        {
            return $@"
Based on this TOEIC passage: {passage}
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
}}
";
        }
        public string GetAnalyzeMistakePrompt(
            QuizSetResponseDto quizSet
            , QuizResponseDto quiz
            , string answersText
            , ResponseUserMistakeDto mistake)
        {
            return $@"
You are an expert TOEIC tutor.
This is a TOEIC practice quiz with the following details:
Topic: {quizSet.Title}
TOIEC part: {quiz.TOEICPart}
Point range: {quizSet.DifficultyLevel}
Question: {quiz.QuestionText}
Answer options : {answersText}
User's wrong answer (maybe user not answer): {mistake.UserAnswer}

Generate ONE single weakpoint(weak area of skill) out of this question and ONE single advice for the user how to improve in this area.

Return in JSON:
{{
  ""WeakPoint"": ""..."",
  ""Advice"": ""...""
}}
";
        }
        public string GetFixWeakPointPrompt()
        {
            return $@"";
        }
    }
}
