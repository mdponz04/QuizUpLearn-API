using BusinessLogic.DTOs;
using BusinessLogic.DTOs.AiDtos;
using BusinessLogic.DTOs.QuizDtos;
using BusinessLogic.DTOs.QuizGroupItemDtos;
using BusinessLogic.DTOs.QuizSetDtos;
using BusinessLogic.DTOs.UserMistakeDtos;
using Repository.Entities;
using static Microsoft.Extensions.Logging.EventSource.LoggingEventSource;

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
        public const string ANALYZE_MISTAKE = "AnalyzeMistakeGeneration";
    }
    public class PromptGenerateQuizSetHelper
    {
        public PromptGenerateQuizSetHelper()
        {
        }

        public string GetValidationPromptAsync(QuizResponseDto quiz
            , List<ResponseAnswerOptionDto> options
            , string groupPassage
            , string groupAudioScript
            , string groupImageDescription
            , string grammar
            , string vocabKeyword)
        {
            return $@"
You are an expert TOEIC test validator.
Review this quiz for correctness and clarity.

### Quiz Context:
Toeic part: {quiz.TOEICPart},
Grammar focus: {grammar},
Vocabulary keyword: {vocabKeyword}.

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
6. Does the question text have grammar or vocabulary keyword if not then it invalid.

Return only 1 fields as JSON structure:
{{ 
    ""IsValid"": true/false
}}
";
        }

        //updated
        public string GetQuizSetPart1Prompt(AiGenerateQuizRequestDto inputData, string previousImageDescription, string previousQuestionText, string keyword, string grammar)
        {
            return $@"
Topic: '{inputData.Topic}'.
Description: Focus on TOEIC Part 1.

Vocabulary keyword: '{keyword}'
Grammar: '{grammar}'

Content must have vocabulary keyword and grammar.

Question should describe a photo scene with one correct answer among four choices. 
The image should be described in detail with atleast 50 words(no need to add vocabulary keyword and grammar in the description).

Avoid previous image description(if any): {previousImageDescription}

Generate ONE question that matches this theme.

Avoid previous question text(if any): {previousQuestionText}

Need to return 3 field:
- ImageDescription: A detailed description of the image related to the question.
- QuestionText: The question text (must using '{keyword}' vocabulary keyword and '{grammar}' grammar).
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
        //updated
        public string GetQuizSetPart2Prompt(AiGenerateQuizRequestDto inputData, string previousQuestionText, string keyword, string grammar)
        {
            return $@"
Topic: '{inputData.Topic}'.
Description: Focus on TOEIC Part 2, 

Vocabulary keyword: '{keyword}'
Grammar: '{grammar}'

Question have 3 answers, each answer should have different context then the correct one is the answer that fit with the topic, you have to generate question text, answer options.

Question text must use vocabulary keyword and grammar.

Generate ONE question that matches this theme.

Avoid previous question text(if any): {previousQuestionText}

Need to return 2 field:
- QuestionText: The question text (must using '{keyword}' vocabulary keyword and '{grammar}' grammar).
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
        //updated
        public string GetPart3AudioPrompt(AiGenerateQuizRequestDto inputData, string previousAudioScript, string keyword, string grammar)
        {
            return $@"
Generate a TOEIC audio topic: '{inputData.Topic}'.
Description: Focus on TOEIC Part 3, 

Vocabulary keyword: '{keyword}'
Grammar: '{grammar}'

Content must have vocabulary keyword and grammar.

Avoid the previous audio script(if it is not null): {previousAudioScript}

Generate ONE generate audio script that contain a short conservation between TWO people (different gender) and matches this theme (must using '{keyword}' vocabulary keyword and '{grammar}' grammar).
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
        //updated
        public string GetPart3QuizPrompt(string audioScript, string previousQuizText, string keyword, string grammarName)
        {
            return $@"
Based on this audio conversation script: {audioScript}

Vocabulary keyword: '{keyword}'
Grammar: '{grammarName}'
Focus on TOEIC Part 3

Content must have vocabulary keyword and grammar.

Generate ONE question with 3 wrong answers and 1 correct answer.

Avoid the previous question text(if it is not null): {previousQuizText}

Need to return 2 field:
- QuestionText: The question text (Must using Vocabulary keyword: '{keyword}' & Grammar: '{grammarName}').
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
        //updated
        public string GetPart4AudioPrompt(AiGenerateQuizRequestDto inputData, string previousAudioScript, string keyword, string grammar)
        {
            return $@"
Generate a TOEIC audio topic: '{inputData.Topic}'.
Description: Focus on TOEIC Part 4

Vocabulary keyword: '{keyword}'
Grammar: '{grammar}'

Content must have vocabulary keyword and grammar.

Avoid the previous audio script(if it is not null): {previousAudioScript}

Generate ONE audio script that contain a short monologue (announcement/speech) matches this theme(must using Vocabulary keyword: '{keyword}' & Grammar: '{grammar}').

Only return in this structure:
{{
  ""AudioScript"": ""...""
}}
";
        }
        //updated
        public string GetPart4QuizPrompt(string audioScript, string previousQuizText, string keyword, string grammarName)
        {
            return $@"
Based on this audio script: {audioScript}


Vocabulary keyword: '{keyword}'
Grammar: '{grammarName}'
Focus on TOEIC Part 4

Content must have vocabulary keyword and grammar.

Generate ONE question with 3 wrong answers and 1 correct answer matches the theme.

Avoid the previous question text(if it is not null): {previousQuizText}

Need to return 2 field:
- QuestionText: The question text(Must using Vocabulary keyword: '{keyword}' & Grammar: '{grammarName}').
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
        //updated
        public string GetPart5Prompt(AiGenerateQuizRequestDto inputData, string previousQuestionText, string keyword, string grammar)
        {
            return $@"
Topic: '{inputData.Topic}'.
Description: Focus on TOEIC Part 5 - in this part the question will be an incomplete sentence with 4 answer to fill in.

Vocabulary keyword: '{keyword}'
Grammar: '{grammar}'

Content must have vocabulary keyword and grammar.

Generate ONE question that matches this theme.

Avoid previous question text(if any): {previousQuestionText}

Need to return 2 field:
- QuestionText: The question text(Must using Vocabulary keyword: '{keyword}' & Grammar: '{grammar}').
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
        //updated
        public string GetPart6PassagePrompt(AiGenerateQuizRequestDto inputData, string previousPassages, string keyword, string grammar)
        {
            return $@"
Generate a TOEIC passage: 
- Title: {inputData.Topic}.
- Description: Focus on TOEIC Part 6

Vocabulary keyword: '{keyword}'
Grammar: '{grammar}'

Content must have vocabulary keyword and grammar.

Avoid the previous passage(if it is not null): {previousPassages}

Generate ONE passage match the theme - generate fully passage without any blanks (at least 75 words with 6 sentences and must using Vocabulary keyword: '{keyword}' & Grammar: '{grammar}').

Only return in this structure:
{{
  ""Passage"": ""..."",
}}
";
        }
        //updated
        public string GetPart6QuizPrompt(string passage, int blankNumber, string usedBlanks, string keyword, string grammarName)
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
        //updated
        public string GetPart7PassagePrompt(AiGenerateQuizRequestDto inputData, string previousPassages, string keyword, string grammar)
        {
            return $@"
Generate a TOEIC Part 7 reading passage about '{inputData.Topic}' 

Vocabulary keyword: '{keyword}'
Grammar: '{grammar}'

Content must have vocabulary keyword and grammar above.

Avoid repeating previous passages: {previousPassages} (Must use Vocabulary keyword: '{keyword}' & Grammar: '{grammar}').
Length: around 120-150 words.

Return only JSON:
{{
  ""Passage"": ""...""
}}
";
        }
        //updated
        public string GetPart7QuizPrompt(string passage, string previousQuizText, string keyword, string grammarName)
        {
            return $@"
Based on this TOEIC passage: {passage}

Vocabulary keyword: '{keyword}'
Grammar: '{grammarName}'
Focus on TOEIC Part 7

Generate ONE reading comprehension question.

Include 4 options (A–D) with 1 correct answer.

Avoid previous question text(if any): {previousQuizText}
Need to return 2 field:
- QuestionText: The question text(Using Vocabulary keyword: '{keyword}' & Grammar: '{grammarName}').
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
        public string GetAnalyzeMistakeQuizPrompt(
            int index
            , QuizResponseDto quiz
            , string answersText
            , ResponseUserMistakeDto mistake
            , ResponseQuizGroupItemDto quizGroupItem = null!)
        {
            return $@"
Quiz number {index}:
TOIEC part: ""{quiz.TOEICPart}""

Additional materials context (if any):
Passage : ""{quizGroupItem?.PassageText ?? string.Empty}""
Audio Script : ""{quizGroupItem?.AudioScript ?? string.Empty}""
Image Description : ""{quizGroupItem?.ImageDescription ?? string.Empty}""

Question: ""{quiz.QuestionText}""
Answer options : ""{answersText}""

User's wrong answer (maybe user not answer): ""{mistake.UserAnswer}""
";
        }
        public string GetAnalyzeMistakeGeneratePrompt()
        {
            return $@"
You are an expert TOEIC tutor.
Generate ONE single weakpoint out of these questions and ONE single advice for the user how to improve in this area.

You need to translate the weakpoint and advice to Vietnamese language.

Return in JSON:
{{
  ""WeakPoint"": ""..."",
  ""Advice"": ""...""
}}";
        }
        public string GetFixWeakPointPrompt()
        {
            return $@"";
        }
    }
}
