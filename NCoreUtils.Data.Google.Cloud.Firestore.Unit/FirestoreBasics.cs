using System;
using System.Threading;
using System.Threading.Tasks;
using Google.Cloud.Firestore;
using Google.Cloud.Firestore.V1;
using Grpc.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NCoreUtils.Data.Build;
using NCoreUtils.Linq;
using Xunit;

namespace NCoreUtils.Data.Google.Cloud.Firestore.Unit
{
    public class FirestoreBasics : IDisposable
    {
        private readonly IServiceProvider _serviceProvider;

        public FirestoreBasics()
        {
            // FirestoreClient.DefaultEndpoint = "http://127.0.0.1:8287";
            Environment.SetEnvironmentVariable("FIRESTORE_EMULATOR_HOST", "127.0.0.1:8287");
            var builder = new FirestoreDbBuilder
            {
                ProjectId = "test",
                // ChannelCredentials = ChannelCredentials.Insecure,
                // Endpoint = "http://127.0.0.1:8287",
                EmulatorDetection = global::Google.Api.Gax.EmulatorDetection.EmulatorOnly
            };
            _serviceProvider = new ServiceCollection()
                .AddLogging(b => b.ClearProviders().SetMinimumLevel(LogLevel.Debug).AddConsole().AddDebug())
                .AddSingleton(builder.Build())
                .AddSingleton(new DataModelBuilder().Entity<SimpleItem>(b =>
                {
                    b.SetKey(new [] { b.Property(e => e.Id).Property });
                }))
                .AddFirestoreDataRepositoryContext(new FirestoreConfiguration { ProjectId = "test" })
                .AddFirestoreDataRepository<SimpleItem>()
                .BuildServiceProvider();

        }

        private async Task Scoped(Func<IServiceProvider, Task> action)
        {
            using var scope = _serviceProvider.CreateScope();
            await action(scope.ServiceProvider);
        }

        public void Dispose()
        {
            (_serviceProvider as IDisposable)?.Dispose();
        }

        [Fact]
        public Task PersistAndQuery() => Scoped(async serviceProvider =>
        {
            var repo = serviceProvider.GetRequiredService<IDataRepository<SimpleItem, string>>();
            var now = DateTimeOffset.Now;
            var item0 = new SimpleItem(default!, "string", 1, 1.0, true, now);
            var item = await repo.PersistAsync(item0);
            var items = await repo.Items.ToListAsync(CancellationToken.None);
            // Assert.NotNull(item.Id);
        });
    }
}
