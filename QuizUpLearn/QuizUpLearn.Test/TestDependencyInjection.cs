using BusinessLogic.Interfaces;
using BusinessLogic.MappingProfile;
using BusinessLogic.Services;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Repository.Interfaces;

namespace QuizUpLearn.Test
{
    public static class TestDependencyInjection
    {
        public static ServiceProvider BuildServiceProvider()
        {
            var services = new ServiceCollection();

            // Add AutoMapper
            services.AddAutoMapper(cfg => { }, typeof(MappingProfile).Assembly);

            // Add real services that you want to test
            services.AddScoped<IQuizService, QuizService>();
            services.AddScoped<IAnswerOptionService, AnswerOptionService>();
            services.AddScoped<IQuizSetService, QuizSetService>();
            // Add other services as needed

            // Mock repositories - these will be replaced in individual tests
            services.AddScoped<IQuizRepo>(provider => Mock.Of<IQuizRepo>());
            services.AddScoped<IAnswerOptionRepo>(provider => Mock.Of<IAnswerOptionRepo>());
            services.AddScoped<IQuizSetRepo>(provider => Mock.Of<IQuizSetRepo>());
            // Add other repository mocks as needed

            return services.BuildServiceProvider();
        }

        public static ServiceProvider BuildServiceProviderWithMocks(params (Type serviceType, object mockInstance)[] mocks)
        {
            var services = new ServiceCollection();

            // Add AutoMapper
            services.AddAutoMapper(cfg => { }, typeof(MappingProfile).Assembly);

            // Add real services
            services.AddScoped<IQuizService, QuizService>();
            services.AddScoped<IAnswerOptionService, AnswerOptionService>();
            services.AddScoped<IQuizSetService, QuizSetService>();

            // Add provided mocks
            foreach (var (serviceType, mockInstance) in mocks)
            {
                services.AddScoped(serviceType, provider => mockInstance);
            }

            // Add default mocks for any missing dependencies
            services.AddScoped<IQuizRepo>(provider => Mock.Of<IQuizRepo>());
            services.AddScoped<IAnswerOptionRepo>(provider => Mock.Of<IAnswerOptionRepo>());
            services.AddScoped<IQuizSetRepo>(provider => Mock.Of<IQuizSetRepo>());

            return services.BuildServiceProvider();
        }
    }
}
