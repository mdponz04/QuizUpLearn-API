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
            services.ConfigCors();
            services.ConfigRoute();
            services.AddRepository();
            services.AddAutoMapper();
            services.AddServices();

        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="services"></param>
        public static void ConfigCors(this IServiceCollection services)
        {
            services.AddCors(options =>
            {
                options.AddPolicy("AllowAllOrigins",
                    builder =>
                    {
                        builder.AllowAnyOrigin()
                               .AllowAnyHeader()
                               .AllowAnyMethod();
                    });
            });
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
                options.UseSqlServer(configuration.GetConnectionString("DefaultConnection"));
            });
        }

        public static void AddRepository(this IServiceCollection services)
        {
            services.AddScoped<IRoleRepo, RoleRepo>();
            services.AddScoped<IAccountRepo, AccountRepo>();
        }

        public static void AddAutoMapper(this IServiceCollection services)
        {
            // Register AutoMapper with assemblies
            services.AddAutoMapper(cfg => { }, typeof(MappingProfile).Assembly);
        }

        public static void AddServices(this IServiceCollection services)
        {
            services.AddLogging();

            services.AddScoped<IRoleService, RoleService>();
            services.AddScoped<IAccountService, AccountService>();
            services.AddScoped<IIdentityService, IdentityService>();
            services.AddScoped<IUploadService, UploadService>();
        }
    }
}
