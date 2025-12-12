using BusinessLogic.DTOs;
using BusinessLogic.DTOs.GrammarDtos;
using BusinessLogic.DTOs.VocabularyDtos;
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

        public async Task<PaginationResponseDto<(ResponseGrammarDto, ResponseVocabularyDto, string)>> GetUnusedPairVocabularyGrammar()
        {
            var allVocabs = await _vocabularyRepo.GetAllAsync();
            var allGrammars = await _grammarRepo.GetAllAsync();

            List<(ResponseGrammarDto, ResponseVocabularyDto, string)> vocabGrammarUnusedPairs = new();
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
                string part = $"PART{i}";
                foreach (var vocab in allVocabs)
                {
                    foreach (var grammar in allGrammars)
                    {
                        // Check if used together in THIS part
                        bool isUsed = vocab.Quizzes.Any(q => q.GrammarId == grammar.Id && q.TOEICPart == part)
                            ||
                            grammar.Quizzes.Any(q => q.VocabularyId == vocab.Id && q.TOEICPart == part);

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

                            vocabGrammarUnusedPairs.Add((grammarDto, vocabularyDto, part));
                        }
                    }
                }
            }
            
            var paginationRequest = new PaginationRequestDto { Page = 1, PageSize = vocabGrammarUnusedPairs.Count };
            return PaginationResponseDto<(ResponseGrammarDto, ResponseVocabularyDto, string)>.Create(
                paginationRequest, 
                vocabGrammarUnusedPairs.Count, 
                vocabGrammarUnusedPairs);
        }
    }
}
