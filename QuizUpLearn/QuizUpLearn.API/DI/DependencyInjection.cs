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
            // Add Swagger services with XML comments
            services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "Swagger API",
                    Version = "v1"
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

        }

        public static void AddServices(this IServiceCollection services)
        {
            services.AddLogging();
            services.AddHttpClient<AIService>();

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
            services.AddScoped<IAIService, AIService>();
            services.AddScoped<IDashboardService, DashboardService>();
            services.AddScoped<IPlacementQuizSetService, PlacementQuizSetService>();
            services.AddScoped<IUserMistakeService, UserMistakeService>();
            services.AddScoped<IQuizGroupItemService, QuizGroupItemService>();
            services.AddScoped<ITournamentService, TournamentService>();
            services.AddScoped<IUserWeakPointService, UserWeakPointService>();

            // RealtimeGameService phải là Singleton vì dùng static state
            services.AddSingleton<BusinessLogic.Services.RealtimeGameService>();
            
            // OneVsOneGameService - Singleton để dùng Redis state
            services.AddSingleton<BusinessLogic.Interfaces.IOneVsOneGameService, BusinessLogic.Services.OneVsOneGameService>();
            //Singleton worker service
            services.AddSingleton<IWorkerService, WorkerService>();
            services.AddHostedService(sp => (WorkerService)sp.GetRequiredService<IWorkerService>());
            services.AddHostedService<TournamentSchedulerService>();
        }
    }
}
