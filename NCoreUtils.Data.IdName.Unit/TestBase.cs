using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NCoreUtils.Text;
using Xunit;

namespace NCoreUtils.Data
{
    public abstract class TestBase : IDisposable
    {
        readonly IServiceProvider _globalServiceProvider;

        int _isDisposed;

        public TestBase(Action<IConfiguration, DbContextOptionsBuilder> initContext)
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(System.IO.Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .Build();

            _globalServiceProvider = new ServiceCollection()
                .AddLogging(e => e.SetMinimumLevel(LogLevel.Trace).AddConsole().AddDebug())
                .AddDbContext<TestDbContext>(opts => initContext(configuration, opts.EnableSensitiveDataLogging(true)))
                .AddDefaultDataRepositoryContext<TestDbContext>()
                .AddEntityFrameworkCoreDataRepository<Item, int>()
                .AddDataEventHandlers()
                .AddSqlIdNameGeneration<TestDbContext>()
                .AddSingleton<ISimplifier>(Simplifier.Default)
                .BuildServiceProvider(true);
            using (var scope = _globalServiceProvider.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<TestDbContext>();
                dbContext.Database.EnsureCreated();

                var dataEventHandlers = scope.ServiceProvider.GetRequiredService<IDataEventHandlers>();
                dataEventHandlers.AddImplicitObservers();
            }
        }

        void IDisposable.Dispose()
        {
            GC.SuppressFinalize(this);
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (0 == Interlocked.CompareExchange(ref _isDisposed, 1, 0))
            {
                using (var scope = _globalServiceProvider.CreateScope())
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<TestDbContext>();
                    dbContext.Database.EnsureDeleted();
                }
                (_globalServiceProvider as IDisposable)?.Dispose();
            }
        }

        protected async Task ScopedAsync(Func<IServiceProvider, CancellationToken, Task> action)
        {
            using (var scope = _globalServiceProvider.CreateScope())
            {
                await action(scope.ServiceProvider, CancellationToken.None);
            }
        }

        public virtual Task InsertOne() => ScopedAsync(async (serviceProvider, cancellationToken) =>
        {
            var repository = serviceProvider.GetRequiredService<IDataRepository<Item, int>>();
            var item = await repository.PersistAsync(new Item { Name = "tesztérték" });
            Assert.NotNull(item.IdName);
            Assert.Equal("tesztertek", item.IdName);
        });

        public virtual Task InsertTwo() => ScopedAsync(async (serviceProvider, cancellationToken) =>
        {
            var repository = serviceProvider.GetRequiredService<IDataRepository<Item, int>>();
            var item0 = await repository.PersistAsync(new Item { Name = "tesztérték" });
            var item1 = await repository.PersistAsync(new Item { Name = "tesztérték" });
            Assert.NotNull(item0.IdName);
            Assert.Equal("tesztertek", item0.IdName);
            Assert.NotNull(item1.IdName);
            Assert.Equal("tesztertek-1", item1.IdName);
        });
    }
}
