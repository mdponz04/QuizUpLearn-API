using BusinessLogic.DTOs.GrammarDtos;
using BusinessLogic.DTOs.VocabularyDtos;

namespace BusinessLogic.Helpers
{
    public class QuizDifficultyLevelDetermine
    {
        public QuizDifficultyLevelDetermine()
        {
        }

        public async Task<string> DetermineQuizDifficultyLevel(ResponseGrammarDto grammar, ResponseVocabularyDto vocabulary)
        {

            if (grammar == null || vocabulary == null)
                throw new ArgumentException("Grammar or Vocabulary not found");

            var grammarLevel = (int)grammar.GrammarDifficulty;
            var vocabLevel = (int)vocabulary.VocabularyDifficulty;

            var averageLevel = (grammarLevel + vocabLevel) / 2.0;

            return averageLevel switch
            {
                < 0.5 => "easy",
                >= 0.5 and < 1.5 => "medium",
                >= 1.5 => "hard"
            };
        }
    }
}
