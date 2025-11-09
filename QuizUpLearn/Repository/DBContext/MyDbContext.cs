using Microsoft.EntityFrameworkCore;
using Repository.Entities;

namespace Repository.DBContext
{
    public class MyDbContext : DbContext
    {
        public MyDbContext() { }
        public MyDbContext(DbContextOptions<MyDbContext> options) : base(options) { }

        public DbSet<Account> Accounts { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<OtpVerification> OTPVerifications { get; set; }
        public DbSet<QuizSet> QuizSets { get; set; }
        public DbSet<Quiz> Quizzes { get; set; }
        public DbSet<AnswerOption> AnswerOptions { get; set; }
        public DbSet<QuizAttempt> QuizAttempts { get; set; }
        public DbSet<QuizAttemptDetail> QuizAttemptDetails { get; set; }
        public DbSet<Event> Events { get; set; }
        public DbSet<EventParticipant> EventParticipants { get; set; }
        public DbSet<Tournament> Tournaments { get; set; }
        public DbSet<TournamentParticipant> TournamentParticipants { get; set; }
        public DbSet<UserMistake> UserMistakes { get; set; }
        public DbSet<QuizGroupItem> QuizGroupItems { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Account>()
                .HasOne(a => a.User)
                .WithOne(u => u.Account)
                .HasForeignKey<Account>(a => a.UserId);
            modelBuilder.Entity<QuizAttempt>()
                .Property(q => q.Accuracy)
                .HasPrecision(3, 2);
            modelBuilder.Entity<QuizSet>()
                .Property(q => q.AverageScore)
                .HasPrecision(5, 2);
            
            // Configure QuizAttemptDetail foreign key relationships
            modelBuilder.Entity<QuizAttemptDetail>()
                .HasOne(qad => qad.QuizAttempt)
                .WithMany(qa => qa.QuizAttemptDetails)
                .HasForeignKey(qad => qad.QuizAttemptId)
                .OnDelete(DeleteBehavior.Cascade);
                
            modelBuilder.Entity<QuizAttemptDetail>()
                .HasOne(qad => qad.Quiz)
                .WithMany(q => q.QuizAttemptDetails)
                .HasForeignKey(qad => qad.QuizId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
