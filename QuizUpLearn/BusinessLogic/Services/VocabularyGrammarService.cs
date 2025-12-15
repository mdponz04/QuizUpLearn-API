using BusinessLogic.DTOs;
using BusinessLogic.DTOs.GrammarDtos;
using BusinessLogic.DTOs.VocabularyDtos;
using BusinessLogic.Helpers;
using BusinessLogic.Interfaces;
using Repository.Enums;
using Repository.Interfaces;

namespace BusinessLogic.Services
{
    public class VocabularyGrammarService : IVocabularyGrammarService
    {
        private readonly IVocabularyRepo _vocabularyRepo;
        private readonly IGrammarRepo _grammarRepo;

        public VocabularyGrammarService(IVocabularyRepo vocabularyRepo, IGrammarRepo grammarRepo)
        {
            _vocabularyRepo = vocabularyRepo;
            _grammarRepo = grammarRepo;
        }

        public async Task<PaginationResponseDto<GrammarVocabularyResponseDto>> GetUnusedPairVocabularyGrammar()
        {
            var allVocabs = await _vocabularyRepo.GetAllAsync();
            var allGrammars = await _grammarRepo.GetAllAsync();

            List<GrammarVocabularyResponseDto> vocabGrammarUnusedPairs = new();
            List<string> parts = new() {
                QuizPartEnum.PART1.ToString(),
                QuizPartEnum.PART2.ToString(),
                QuizPartEnum.PART3.ToString(),
                QuizPartEnum.PART4.ToString(),
                QuizPartEnum.PART5.ToString(),
                QuizPartEnum.PART6.ToString(),
                QuizPartEnum.PART7.ToString()
            };

            for(int i = 1; i <= 7; i++)
            {
                //Check part
                string part = $"PART{i}";

                foreach (var vocab in allVocabs)
                {
                    // Get any quiz of this vocab (Check vocab) there should be 1 or 0 quiz only
                    var quiz = vocab.Quizzes.FirstOrDefault();
                    if(quiz == null || quiz.TOEICPart != part)
                    {
                        continue;
                    }

                    foreach (var grammar in allGrammars)
                    {
                        bool isUsed = false;
                        //Check grammar
                        if (quiz.GrammarId == grammar.Id)
                        {
                            isUsed = true;
                        }

                        if (!isUsed)
                        {
                            var grammarDto = new ResponseGrammarDto
                            {
                                Id = grammar.Id,
                                Name = grammar.Name,
                                Tense = grammar.Tense,
                                GrammarDifficulty = grammar.GrammarDifficulty,
                                CreatedAt = grammar.CreatedAt,
                                UpdatedAt = grammar.UpdatedAt
                            };

                            var vocabularyDto = new ResponseVocabularyDto
                            {
                                Id = vocab.Id,
                                KeyWord = vocab.KeyWord,
                                VocabularyDifficulty = vocab.VocabularyDifficulty,
                                ToeicPart = vocab.ToeicPart,
                                PassageType = vocab.PassageType,
                                CreatedAt = vocab.CreatedAt,
                                UpdatedAt = vocab.UpdatedAt
                            };

                            vocabGrammarUnusedPairs.Add(new GrammarVocabularyResponseDto
                            {
                                Grammar = grammarDto,
                                Vocabulary = vocabularyDto,
                                Part = part
                            });
                        }
                    }
                }
            }
            
            var paginationResponse = PaginationHelper.CreatePagedResponse(vocabGrammarUnusedPairs, new PaginationRequestDto
            {
            });

            return paginationResponse;
        }
    }
}
