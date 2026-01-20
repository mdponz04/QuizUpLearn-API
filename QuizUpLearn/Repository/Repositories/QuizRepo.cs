using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Repository.DBContext;
using Repository.Entities;
using Repository.Interfaces;

namespace Repository.Repositories
{
    public class QuizRepo : IQuizRepo
    {
        private readonly MyDbContext _context;

        public QuizRepo(MyDbContext context)
        {
            _context = context;
        }

        public async Task<Quiz> CreateQuizAsync(Quiz quiz)
        {
            await _context.Quizzes.AddAsync(quiz);
            await _context.SaveChangesAsync();
            return quiz;
        }

        public async Task<IEnumerable<Quiz>> CreateQuizzesBatchAsync(IEnumerable<Quiz> quizzes)
        {
            var quizzesList = quizzes.ToList();
            await _context.Quizzes.AddRangeAsync(quizzesList);
            await _context.SaveChangesAsync();
            return quizzesList;
        }

        public async Task<Quiz?> GetQuizByIdAsync(Guid id)
        {
            return await _context.Quizzes
                .Include(q => q.QuizGroupItem)
                .Include(q => q.AnswerOptions)
                .Include(q => q.Vocabulary)
                .Include(q => q.Grammar)
                .AsNoTracking()
                .FirstOrDefaultAsync(q => q.Id == id && q.DeletedAt == null);
        }

        public async Task<IEnumerable<Quiz>> GetQuizzesByIdsAsync(IEnumerable<Guid> ids)
        {
            var idsList = ids.ToList();
            return await _context.Quizzes
                .Include(q => q.Vocabulary)
                .Include(q => q.Grammar)
                .Include(q => q.QuizGroupItem)
                .Include(q => q.AnswerOptions)
                .Where(q => idsList.Contains(q.Id) && q.DeletedAt == null)
                .ToListAsync();
        }

        public async Task<IEnumerable<Quiz>> GetAllQuizzesAsync()
        {
            return await _context.Quizzes
                .Include(q => q.Vocabulary)
                .Include(q => q.Grammar)
                .Include(q => q.QuizGroupItem)
                .Include(q => q.AnswerOptions)
                .Where(q => q.DeletedAt == null)
                .ToListAsync();
        }

        public async Task<IEnumerable<Quiz>> GetQuizzesByQuizSetIdAsync(Guid quizSetId)
        {
            return await _context.QuizQuizSets
                .Where(qq => qq.QuizSetId == quizSetId && qq.DeletedAt == null)
                .Join(
                    _context.Quizzes
                    .Include(q => q.Vocabulary)
                    .Include(q => q.Grammar)
                    .Include(q => q.QuizGroupItem)
                    .Include(q => q.AnswerOptions),
                    qq => qq.QuizId,
                    q => q.Id,
                    (qq, q) => q)
                .Where(q => q.DeletedAt == null)
                .Distinct()
                .ToListAsync();
        }

        public async Task<Quiz> UpdateQuizAsync(Guid id, Quiz quiz)
        {
            var existingQuiz = await _context.Quizzes.FindAsync(id);
            if (existingQuiz == null || existingQuiz.DeletedAt != null)
                return null;

            bool hasChanges = false;

            if(quiz.GrammarId != null && existingQuiz.GrammarId != quiz.GrammarId)
            {
                existingQuiz.GrammarId = quiz.GrammarId;
                hasChanges = true;
            }
            if(quiz.VocabularyId != null && existingQuiz.VocabularyId != quiz.VocabularyId)
            {
                existingQuiz.VocabularyId = quiz.VocabularyId;
                hasChanges = true;
            }
            if (!string.IsNullOrEmpty(quiz.QuestionText) && existingQuiz.QuestionText != quiz.QuestionText)
            {
                existingQuiz.QuestionText = quiz.QuestionText;
                hasChanges = true;
            }
            if(!string.IsNullOrEmpty(quiz.CorrectAnswer) && existingQuiz.CorrectAnswer != quiz.CorrectAnswer)
            {
                existingQuiz.CorrectAnswer = quiz.CorrectAnswer;
                hasChanges = true;
            }
            
            if(existingQuiz.AudioURL != quiz.AudioURL)
            {
                existingQuiz.AudioURL = quiz.AudioURL;
                hasChanges = true;
            }
            if(existingQuiz.ImageURL != quiz.ImageURL)
            {
                existingQuiz.ImageURL = quiz.ImageURL;
                hasChanges = true;
            }
            
            if(!string.IsNullOrEmpty(quiz.TOEICPart) && existingQuiz.TOEICPart != quiz.TOEICPart)
            {
                existingQuiz.TOEICPart = quiz.TOEICPart;
                hasChanges = true;
            }
            if(existingQuiz.TimesAnswered != quiz.TimesAnswered)
            {
                existingQuiz.TimesAnswered = quiz.TimesAnswered;
                hasChanges = true;
            }
            if(existingQuiz.TimesCorrect != quiz.TimesCorrect)
            {
                existingQuiz.TimesCorrect = quiz.TimesCorrect;
                hasChanges = true;
            }
            if(existingQuiz.QuizGroupItemId != quiz.QuizGroupItemId)
            {
                existingQuiz.QuizGroupItemId = quiz.QuizGroupItemId;
                hasChanges = true;
            }

            if(quiz.ImageDescription != null && existingQuiz.ImageDescription != quiz.ImageDescription)
            {
                existingQuiz.ImageDescription = quiz.ImageDescription;
                hasChanges = true;
            }
            if(quiz.AudioScript != null && existingQuiz.AudioScript != quiz.AudioScript)
            {
                existingQuiz.AudioScript = quiz.AudioScript;
                hasChanges = true;
            }
            if(existingQuiz.OrderIndex != quiz.OrderIndex)
            {
                existingQuiz.OrderIndex = quiz.OrderIndex;
                hasChanges = true;
            }
            if(existingQuiz.IsActive != quiz.IsActive)
            {
                existingQuiz.IsActive = quiz.IsActive;
                hasChanges = true;
            }

            if(hasChanges)
            {
                existingQuiz.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
            
            return existingQuiz;
        }

        public async Task<bool> SoftDeleteQuizAsync(Guid id)
        {
            var quiz = await _context.Quizzes.FindAsync(id);
            if (quiz == null)
                return false;

            quiz.DeletedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> HardDeleteQuizAsync(Guid id)
        {
            var quiz = await _context.Quizzes.FindAsync(id);
            if (quiz == null)
                return false;

            _context.Quizzes.Remove(quiz);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> RestoreQuizAsync(Guid id)
        {
            var quiz = await _context.Quizzes
                .FirstOrDefaultAsync(q => q.Id == id && q.DeletedAt != null);
            
            if (quiz == null) return false;
            
            quiz.DeletedAt = null;
            await _context.SaveChangesAsync();
            return true;
        }


        public async Task<IEnumerable<Quiz>> GetByGrammarIdAndVocabularyId(Guid grammarId, Guid vocabId)
        {
            return await _context.Quizzes.
                Where(q => q.GrammarId == grammarId
                    && q.VocabularyId == vocabId
                    && q.DeletedAt == null)
                .Include(q => q.Vocabulary)
                .Include(q => q.Grammar)
                .Include(q => q.QuizGroupItem)
                .ToListAsync();
        }

        public async Task<bool> HardDeleteQuizzesBatchAsync(IEnumerable<Quiz> quizzes)
        {
            _context.Quizzes.RemoveRange(quizzes.ToList());
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<Quiz>> GetQuizzesByPartAsync(string toeicPart)
        {
            return await _context.Quizzes
                .Where(q => q.TOEICPart == toeicPart && q.DeletedAt == null && q.IsActive)
                .ToListAsync();
        }
    }
}
