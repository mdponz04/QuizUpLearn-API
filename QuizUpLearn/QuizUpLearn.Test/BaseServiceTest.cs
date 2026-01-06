using Microsoft.Extensions.DependencyInjection;

namespace QuizUpLearn.Test
{
    public abstract class BaseServiceTest : IDisposable
    {
        public ServiceProvider ServiceProvider { get; private set; }

        public BaseServiceTest()
        {
            ServiceProvider = TestDependencyInjection.BuildServiceProvider();
        }

        public T GetService<T>() where T : notnull
        {
            return ServiceProvider.GetRequiredService<T>();
        }

        public virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                ServiceProvider?.Dispose();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
