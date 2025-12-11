using Microsoft.EntityFrameworkCore;
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

        public async Task<IEnumerable<Quiz>> GetActiveQuizzesAsync()
        {
            return await _context.Quizzes
                .Include(q => q.Vocabulary)
                .Include(q => q.Grammar)
                .Include(q => q.QuizGroupItem)
                .Include(q => q.AnswerOptions)
                .Where(q => q.IsActive && q.DeletedAt == null)
                .ToListAsync();
        }

        public async Task<Quiz> UpdateQuizAsync(Guid id, Quiz quiz)
        {
            var existingQuiz = await _context.Quizzes.FindAsync(id);
            if (existingQuiz == null || existingQuiz.DeletedAt != null)
                return null;

            if(existingQuiz.GrammarId != quiz.GrammarId)
                existingQuiz.GrammarId = quiz.GrammarId;
            if(existingQuiz.VocabularyId != quiz.VocabularyId)
                existingQuiz.VocabularyId = quiz.VocabularyId;
            if (!string.IsNullOrEmpty(quiz.QuestionText))
                existingQuiz.QuestionText = quiz.QuestionText;
            if(!string.IsNullOrEmpty(quiz.CorrectAnswer))
                existingQuiz.CorrectAnswer = quiz.CorrectAnswer;
            if(!string.IsNullOrEmpty(quiz.AudioURL))
                existingQuiz.AudioURL = quiz.AudioURL;
            if(!string.IsNullOrEmpty(quiz.ImageURL))
                existingQuiz.ImageURL = quiz.ImageURL;
            if(!string.IsNullOrEmpty(quiz.TOEICPart))
                existingQuiz.TOEICPart = quiz.TOEICPart;
            if(existingQuiz.TimesAnswered > 0)
                existingQuiz.TimesAnswered = quiz.TimesAnswered;
            if(existingQuiz.TimesCorrect > 0)
                existingQuiz.TimesCorrect = quiz.TimesCorrect;
            if(existingQuiz.QuizGroupItemId != quiz.QuizGroupItemId)
                existingQuiz.QuizGroupItemId = quiz.QuizGroupItemId;

            existingQuiz.OrderIndex = quiz.OrderIndex;

            quiz.UpdatedAt = DateTime.UtcNow;
            _context.Quizzes.Update(existingQuiz);
            await _context.SaveChangesAsync();
            return quiz;
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

        public async Task<bool> QuizExistsAsync(Guid id)
        {
            return await _context.Quizzes.AnyAsync(q => q.Id == id && q.DeletedAt == null);
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
    }
}
