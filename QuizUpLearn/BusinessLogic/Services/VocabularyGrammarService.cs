using BusinessLogic.DTOs;
using BusinessLogic.DTOs.GrammarDtos;
using BusinessLogic.DTOs.VocabularyDtos;
using BusinessLogic.Helpers;
using BusinessLogic.Interfaces;
using Repository.Entities;
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

        public async Task<PaginationResponseDto<GrammarVocabularyResponseDto>> GetUnusedPairVocabularyGrammar(PaginationRequestDto pagination = null!)
        {
            if(pagination == null)
            {
                pagination = new();
            }
            var allVocabs = await _vocabularyRepo.GetAllAsync();
            var allGrammars = await _grammarRepo.GetAllAsync();

            List<GrammarVocabularyResponseDto> vocabGrammarUnusedPairs = new();

            for(int i = 1; i <= 7; i++)
            {
                string part = $"PART{i}";
                var vocabsByPart = allVocabs
                    .Where(v => string.Equals(v.ToeicPart, part, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(v.ToeicPart, i.ToString(), StringComparison.OrdinalIgnoreCase))
                    .ToList();

                foreach (var vocab in vocabsByPart)
                {
                    var quizzes = vocab.Quizzes;
                    if(quizzes.Count == 0)
                    {
                        foreach (var grammar in allGrammars)
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
                    else
                    {
                        foreach (var grammar in allGrammars)
                        {
                            var isExisted = quizzes.Any(q => q.GrammarId == grammar.Id);
                            if(isExisted) continue;

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

            vocabGrammarUnusedPairs = ApplySearch(vocabGrammarUnusedPairs, pagination.SearchTerm);
            vocabGrammarUnusedPairs = ApplyFilters(vocabGrammarUnusedPairs, ExtractFilterValues(pagination));

            var paginationResponse = PaginationHelper.CreatePagedResponse(vocabGrammarUnusedPairs, pagination);

            return paginationResponse;
        }

        private static List<GrammarVocabularyResponseDto> ApplySearch(List<GrammarVocabularyResponseDto> list, string? searchTerm)
        {
            if (string.IsNullOrEmpty(searchTerm))
                return list;

            var normalizedSearchTerm = searchTerm.ToLower();
            var query = list.AsQueryable();

            query = query.Where(gv =>
                (gv.Grammar != null
                && !string.IsNullOrEmpty(gv.Grammar.Name)
                && gv.Grammar.Name.ToLower().Contains(normalizedSearchTerm)) ||
                (gv.Vocabulary != null
                && !string.IsNullOrEmpty(gv.Vocabulary.KeyWord)
                && gv.Vocabulary.KeyWord.ToLower().Contains(normalizedSearchTerm)));

            return query.ToList();
        }

        private string? ExtractFilterValues(PaginationRequestDto pagination)
        {
            var jsonExtractHelper = new JsonExtractHelper();
            if (pagination.Filters == null)
                return (null);

            string? part = jsonExtractHelper.GetStringFromFilter(pagination.Filters, "part");

            return (part);
        }

        private static List<GrammarVocabularyResponseDto> ApplyFilters(
            List<GrammarVocabularyResponseDto> list,
            string? part = null)
        {
            if(part != null)
            {
                var normalizedFilterPart = part.StartsWith("PART", StringComparison.OrdinalIgnoreCase) 
                    ? part.ToUpper() 
                    : $"PART{part}";
                
                list = list.Where(gv => gv.Part != null && 
                                        gv.Part.Equals(normalizedFilterPart, StringComparison.OrdinalIgnoreCase))
                          .ToList();
            }

            return list;
        }
    }
}
