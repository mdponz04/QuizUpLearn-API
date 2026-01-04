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
        public DbSet<TournamentQuizSet> TournamentQuizSets { get; set; }
        public DbSet<UserWeakPoint> UserWeakPoints { get; set; }
        public DbSet<Subscription> Subscriptions { get; set; }
        public DbSet<PaymentTransaction> PaymentTransactions { get; set; }
        public DbSet<SubscriptionPlan> SubscriptionPlans { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<UserNotification> UserNotifications { get; set; }
        public DbSet<AppSetting> AppSettings { get; set; }
        public DbSet<Grammar> Grammars { get; set; }
        public DbSet<Vocabulary> Vocabularies { get; set; }
        public DbSet<QuizQuizSet> QuizQuizSets { get; set; }
        public DbSet<QuizReport> QuizReports { get; set; }
        public DbSet<QuizSetComment> QuizSetComments { get; set; }
        public DbSet<UserQuizSetFavorite> UserQuizSetFavorites { get; set; }
        public DbSet<UserQuizSetLike> UserQuizSetLikes { get; set; }
        public DbSet<Badge> Badges { get; set; }
        public DbSet<UserBadge> UserBadges { get; set; }
        public DbSet<BadgeRule> BadgeRules { get; set; }
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
