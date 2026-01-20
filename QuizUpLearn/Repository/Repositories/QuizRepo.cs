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

            if(quiz.GrammarId != null)
                existingQuiz.GrammarId = quiz.GrammarId;
            if(quiz.VocabularyId != null)
                existingQuiz.VocabularyId = quiz.VocabularyId;
            if (!string.IsNullOrEmpty(quiz.QuestionText))
                existingQuiz.QuestionText = quiz.QuestionText;
            if(!string.IsNullOrEmpty(quiz.CorrectAnswer))
                existingQuiz.CorrectAnswer = quiz.CorrectAnswer;
            
            existingQuiz.AudioURL = quiz.AudioURL;
            existingQuiz.ImageURL = quiz.ImageURL;
            
            if(!string.IsNullOrEmpty(quiz.TOEICPart))
                existingQuiz.TOEICPart = quiz.TOEICPart;
            if(existingQuiz.TimesAnswered > 0)
                existingQuiz.TimesAnswered = quiz.TimesAnswered;
            if(existingQuiz.TimesCorrect > 0)
                existingQuiz.TimesCorrect = quiz.TimesCorrect;
            if(existingQuiz.QuizGroupItemId != null)
                existingQuiz.QuizGroupItemId = quiz.QuizGroupItemId;

            if(quiz.ImageDescription != null)
                existingQuiz.ImageDescription = quiz.ImageDescription;
            if(quiz.AudioScript != null)
                existingQuiz.AudioScript = quiz.AudioScript;
            existingQuiz.OrderIndex = quiz.OrderIndex;
            existingQuiz.IsActive = quiz.IsActive;

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
