using System;
using System.Threading.Tasks;
using Google.Cloud.Firestore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NCoreUtils.Data.Build;

namespace NCoreUtils.Data.Google.Cloud.Firestore.Unit
{
    public abstract class TestBase : IDisposable
    {
        protected static DateTimeOffset RoundToMilliseconds(DateTimeOffset source)
        {
            var utcTicks = source.UtcTicks;
            var diff = utcTicks % TimeSpan.TicksPerMillisecond;
            return new DateTimeOffset(utcTicks - diff + (diff > TimeSpan.TicksPerMillisecond / 2 ? TimeSpan.TicksPerMillisecond : 0), TimeSpan.Zero);
        }

        private readonly IServiceProvider _serviceProvider;

        public TestBase()
        {
            // Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", "/etc/google/hydra-hosting.json");
            Environment.SetEnvironmentVariable("FIRESTORE_EMULATOR_HOST", "127.0.0.1:8287");
            var builder = new FirestoreDbBuilder
            {
                ProjectId = "test",
                EmulatorDetection = global::Google.Api.Gax.EmulatorDetection.EmulatorOnly
            };
            _serviceProvider = new ServiceCollection()
                .AddLogging(b => b.ClearProviders().SetMinimumLevel(LogLevel.Debug).AddConsole().AddDebug())
                .AddSingleton(builder.Build())
                .AddSingleton(new DataModelBuilder().Entity<SimpleItem>(b =>
                {
                    b.SetKey(new [] { b.Property(e => e.Id).Property });
                }))
                .AddFirestoreDataRepositoryContext(new FirestoreConfiguration { ProjectId = "hydra-hosting" })
                .AddFirestoreDataRepository<SimpleItem>()
                .BuildServiceProvider();

        }

        protected async Task Scoped(Func<IServiceProvider, Task> action)
        {
            using var scope = _serviceProvider.CreateScope();
            await action(scope.ServiceProvider);
        }

        public void Dispose()
        {
            (_serviceProvider as IDisposable)?.Dispose();
        }
    }
}