using BusinessLogic.Interfaces;
using BusinessLogic.MappingProfile;
using BusinessLogic.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Repository.DBContext;
using Repository.Interfaces;
using Repository.Repositories;

namespace QuizUpLearn.API.DI
{
    public static class DependencyInjection
    {
        public static void AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            services.ConfigSwagger(configuration);
            services.AddDatabase(configuration);
            services.ConfigRoute();
            services.AddRepository();
            services.AddAutoMapper();
            services.AddServices();
            services.AddHttpClient();
            services.AddHttpClient<IMailerSendService, MailerSendService>();

        }

        public static void ConfigRoute(this IServiceCollection services)
        {
            services.Configure<RouteOptions>(options =>
            {
                options.LowercaseUrls = true;
            });
        }

        public static void ConfigSwagger(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "QuizUpLearn API", Version = "v1.1" });
                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT"
                });

                c.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        Array.Empty<string>()
                    }
                });
            });
        }

        public static void AddDatabase(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddDbContext<MyDbContext>(options =>
            {
                options.UseNpgsql(configuration.GetConnectionString("PostgreSqlConnection"));
            });
        }

        public static void AddAutoMapper(this IServiceCollection services)
        {
            // Register AutoMapper with assemblies
            services.AddAutoMapper(cfg => { }, typeof(MappingProfile).Assembly);
        }

        public static void AddRepository(this IServiceCollection services)
        {
            services.AddScoped<IRoleRepo, RoleRepo>();
            services.AddScoped<IAccountRepo, AccountRepo>();
            services.AddScoped<IUserRepo, UserRepo>();
            services.AddScoped<IOtpVerificationRepo, OtpVerificationRepo>();
            services.AddScoped<IQuizSetRepo, QuizSetRepo>();
            services.AddScoped<IQuizRepo, QuizRepo>();
            services.AddScoped<IQuizAttemptRepo, QuizAttemptRepo>();
            services.AddScoped<IQuizAttemptDetailRepo, QuizAttemptDetailRepo>();
            services.AddScoped<IAnswerOptionRepo, AnswerOptionRepo>();
            services.AddScoped<IUserMistakeRepo, UserMistakeRepo>();
            services.AddScoped<IQuizGroupItemRepo, QuizGroupItemRepo>();
            services.AddScoped<ITournamentRepo, TournamentRepo>();
            services.AddScoped<ITournamentQuizSetRepo, TournamentQuizSetRepo>();
            services.AddScoped<ITournamentParticipantRepo, TournamentParticipantRepo>();
            services.AddScoped<IUserWeakPointRepo, UserWeakPointRepo>();
            services.AddScoped<IPaymentTransactionRepo,PaymentTransactionRepo>();
            services.AddScoped<ISubscriptionRepo,SubscriptionRepo>();
            services.AddScoped<ISubscriptionPlanRepo,SubscriptionPlanRepo>();
            services.AddScoped<IEventRepo, EventRepo>();
            services.AddScoped<IEventParticipantRepo, EventParticipantRepo>();
            services.AddScoped<IAppSettingRepo, AppSettingRepo>();
            services.AddScoped<IAdminDashboardRepo, AdminDashboardRepo>();
            services.AddScoped<IQuizQuizSetRepo, QuizQuizSetRepo>();
            services.AddScoped<IGrammarRepo, GrammarRepo>();
            services.AddScoped<IVocabularyRepo, VocabularyRepo>();
            services.AddScoped<IQuizReportRepo, QuizReportRepo>();
            services.AddScoped<IQuizSetCommentRepo, QuizSetCommentRepo>();
            services.AddScoped<IUserQuizSetFavoriteRepo, UserQuizSetFavoriteRepo>();
            services.AddScoped<IUserQuizSetLikeRepo, UserQuizSetLikeRepo>();
            services.AddScoped<INotificationRepo, NotificationRepo>();
            services.AddScoped<IUserNotificationRepo, UserNotificationRepo>();
        }

        public static void AddServices(this IServiceCollection services)
        {
            services.AddLogging();
            services.AddHttpClient<IAIService, AIService>();

            services.AddScoped<IRoleService, RoleService>();
            services.AddScoped<IAccountService, AccountService>();
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IIdentityService, IdentityService>();
            services.AddScoped<IUploadService, UploadService>();
            services.AddScoped<IMailerSendService, MailerSendService>();
            services.AddScoped<IQuizSetService, QuizSetService>();
            services.AddScoped<IQuizService, QuizService>();
            services.AddScoped<IQuizAttemptService, QuizAttemptService>();
            services.AddScoped<IQuizAttemptDetailService, QuizAttemptDetailService>();
            services.AddScoped<IAnswerOptionService, AnswerOptionService>();
            services.AddScoped<IDashboardService, DashboardService>();
            services.AddScoped<IPlacementQuizSetService, PlacementQuizSetService>();
            services.AddScoped<IUserMistakeService, UserMistakeService>();
            services.AddScoped<IQuizGroupItemService, QuizGroupItemService>();
            services.AddScoped<ITournamentService, TournamentService>();
            services.AddScoped<IUserWeakPointService, UserWeakPointService>();
            services.AddScoped<IPaymentTransactionService, PaymentTransactionService>();
            services.AddScoped<ISubscriptionService, SubscriptionService>();
            services.AddScoped<ISubscriptionPlanService, SubscriptionPlanService>();
            services.AddScoped<IPaymentService, PaymentService>();
            services.AddScoped<IBuySubscriptionService, BuySubscriptionService>();
            services.AddScoped<IEventService, EventService>();
            services.AddScoped<ISubscriptionUsageService, SubscriptionUsageService>();
            services.AddScoped<IAppSettingService, AppSettingService>();
            services.AddScoped<IAdminDashboardService, AdminDashboardService>();
            services.AddScoped<IQuizQuizSetService, QuizQuizSetService>();
            services.AddScoped<IGrammarService, GrammarService>();
            services.AddScoped<IVocabularyService, VocabularyService>();
            services.AddScoped<IVocabularyGrammarService, VocabularyGrammarService>();
            services.AddScoped<IQuizReportService, QuizReportService>();
            services.AddScoped<IQuizSetCommentService, QuizSetCommentService>();
            services.AddScoped<IUserQuizSetFavoriteService, UserQuizSetFavoriteService>();
            services.AddScoped<IUserQuizSetLikeService, UserQuizSetLikeService>();
            services.AddScoped<INotificationService, NotificationService>();
            services.AddScoped<IUserNotificationService, UserNotificationService>();

            // RealtimeGameService - Singleton để dùng Redis state
            services.AddSingleton<BusinessLogic.Interfaces.IRealtimeGameService, BusinessLogic.Services.RealtimeGameService>();
            
            // OneVsOneGameService - Singleton để dùng Redis state
            services.AddScoped<BusinessLogic.Interfaces.IOneVsOneGameService, BusinessLogic.Services.OneVsOneGameService>();
            
            // Worker Service
            services.AddSingleton<IWorkerService, WorkerService>();
            services.AddHostedService(sp => (WorkerService)sp.GetRequiredService<IWorkerService>());
            
            // Background Schedulers with interfaces for loose coupling
            services.AddSingleton<IEventSchedulerService, EventSchedulerService>();
            services.AddHostedService(sp => (EventSchedulerService)sp.GetRequiredService<IEventSchedulerService>());
            services.AddHostedService<TournamentSchedulerService>();
        }
    }
}
