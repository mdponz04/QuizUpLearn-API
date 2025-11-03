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

        public async Task<Quiz> GetQuizByIdAsync(Guid id)
        {
            return await _context.Quizzes
                .Include(q => q.AnswerOptions)
                .FirstOrDefaultAsync(q => q.Id == id && q.DeletedAt == null);
        }

        public async Task<IEnumerable<Quiz>> GetAllQuizzesAsync()
        {
            return await _context.Quizzes
                .Include(q => q.AnswerOptions)
                .Where(q => q.DeletedAt == null)
                .ToListAsync();
        }

        public async Task<IEnumerable<Quiz>> GetQuizzesByQuizSetIdAsync(Guid quizSetId)
        {
            return await _context.Quizzes
                .Include(q => q.AnswerOptions)
                .Where(q => q.QuizSetId == quizSetId && q.DeletedAt == null)
                .ToListAsync();
        }

        public async Task<IEnumerable<Quiz>> GetActiveQuizzesAsync()
        {
            return await _context.Quizzes
                .Include(q => q.AnswerOptions)
                .Where(q => q.IsActive && q.DeletedAt == null)
                .ToListAsync();
        }

        public async Task<Quiz> UpdateQuizAsync(Guid id, Quiz quiz)
        {
            var existingQuiz = await _context.Quizzes.FindAsync(id);
            if (existingQuiz == null || existingQuiz.DeletedAt != null)
                return null;

            if(!string.IsNullOrEmpty(quiz.QuestionText))
                existingQuiz.QuestionText = quiz.QuestionText;
            if(!string.IsNullOrEmpty(quiz.CorrectAnswer))
                existingQuiz.CorrectAnswer = quiz.CorrectAnswer;
            if(!string.IsNullOrEmpty(quiz.GroupId))
                existingQuiz.GroupId = quiz.GroupId;
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
    }
}
