using System;
using System.Data;
using System.Linq;
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
                .AddLogging(e => e.SetMinimumLevel(LogLevel.Error).AddConsole().AddDebug())
                .AddDbContext<TestDbContext>(opts => initContext(configuration, opts.EnableSensitiveDataLogging(false)))
                .AddDefaultDataRepositoryContext<TestDbContext>()
                .AddEntityFrameworkCoreDataRepository<Item, int>()
                .AddEntityFrameworkCoreDataRepository<Item2, int>()
                .AddDataEventHandlers()
                // FIXME
                // .AddSqlIdNameGeneration<TestDbContext>()
                .AddSingleton<ISimplifier>(Simplifier.Default)
                .BuildServiceProvider(true);
            using var scope = _globalServiceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<TestDbContext>();
            dbContext.Database.EnsureDeleted();
            dbContext.Database.EnsureCreated();

            var dataEventHandlers = scope.ServiceProvider.GetRequiredService<IDataEventHandlers>();
            dataEventHandlers.AddImplicitObservers();
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
                // using (var scope = _globalServiceProvider.CreateScope())
                // {
                //     var dbContext = scope.ServiceProvider.GetRequiredService<TestDbContext>();
                //     dbContext.Database.EnsureDeleted();
                // }
                (_globalServiceProvider as IDisposable)?.Dispose();
            }
        }

        protected void Scoped(Action<IServiceProvider> action)
        {
            using var scope = _globalServiceProvider.CreateScope();
            action(scope.ServiceProvider);
        }

        protected async Task ScopedAsync(Func<IServiceProvider, CancellationToken, Task> action)
        {
            using var scope = _globalServiceProvider.CreateScope();
            await action(scope.ServiceProvider, CancellationToken.None);
        }

        public virtual Task InsertOne() => ScopedAsync(async (serviceProvider, cancellationToken) =>
        {
            var repository = serviceProvider.GetRequiredService<IDataRepository<Item, int>>();
            var item = await repository.PersistAsync(new Item { Name = "tesztérték" });
            Assert.NotNull(item.IdName);
            Assert.Equal("tesztertek", item.IdName);
            if (repository is EntityFrameworkCore.DataRepository repo)
            {
                var nextName = await repository.Context.TransactedAsync(
                    IsolationLevel.Serializable,
                    () => repository.Items
                        .GenerateIdNameAsync(
                            serviceProvider,
                            repo.IdNameDescription,
                            "tesztérték",
                            null,
                            cancellationToken),
                    cancellationToken);
                Assert.Equal("tesztertek-1", nextName);
            }
            // UPDATE CHECK
            item.Name = "új érték";
            var item1 = await repository.PersistAsync(item, cancellationToken);
            // IMPLICIT ATTACH
            var itemX = await repository.PersistAsync(new Item { Id = item1.Id, Name = item1.Name, IdName = item1.IdName }, cancellationToken);
            Assert.Equal("tesztertek", item1.IdName);
            // LIST CHECK
            var items = await NCoreUtils.Linq.QueryableExtensions.ToListAsync(repository.Items, cancellationToken);
            Assert.True(1 == items.Count);
            Assert.Single(items, i => i.IdName == "tesztertek");
            Assert.Equal(1, await NCoreUtils.Linq.QueryableExtensions.CountAsync(repository.Items, cancellationToken));
            // DELETE CHECK
            await repository.RemoveAsync(item1, true, cancellationToken);
            items = await NCoreUtils.Linq.QueryableExtensions.ToListAsync(repository.Items, cancellationToken);
            Assert.Empty(items);
            // DETATCHED DELETE
            await Assert.ThrowsAsync<InvalidOperationException>(() => repository.RemoveAsync(new Item { Id = 20, Name = "érték" }, true, cancellationToken));
        });

        public virtual Task InsertTwo() => ScopedAsync(async (serviceProvider, cancellationToken) =>
        {
            var repository = serviceProvider.GetRequiredService<IDataRepository<Item, int>>();
            await repository.Context.TransactedAsync(IsolationLevel.ReadCommitted, async () =>
            {
                var item0 = await repository.PersistAsync(new Item { Name = "tesztérték" }, cancellationToken);
                var item1 = await repository.PersistAsync(new Item { Name = "tesztérték" }, cancellationToken);
                Assert.NotNull(item0.IdName);
                Assert.Equal("tesztertek", item0.IdName);
                Assert.NotNull(item1.IdName);
                Assert.Equal("tesztertek-1", item1.IdName);
            }, cancellationToken);
        });

        public virtual Task InsertOneWithForeign() => ScopedAsync(async (serviceProvider, cancellationToken) =>
        {
            var repository = serviceProvider.GetRequiredService<IDataRepository<Item2, int>>();
            var item0 = await repository.PersistAsync(new Item2 { Name = "tesztérték", ForeignId = 1 });
            Assert.NotNull(item0.IdName);
            Assert.Equal("tesztertek", item0.IdName);
            var item1 = await repository.PersistAsync(new Item2 { Name = "tesztérték", ForeignId = 2 });
            Assert.NotNull(item1.IdName);
            Assert.Equal("tesztertek", item1.IdName);

            if (repository is EntityFrameworkCore.DataRepository repo)
            {
                var nextName = await repository.Items.GenerateIdNameAsync(serviceProvider, repo.IdNameDescription, "tesztérték", new { ForeignId = 1 }, cancellationToken);
                Assert.Equal("tesztertek-1", nextName);
                await Assert.ThrowsAsync<ArgumentNullException>(() => repository.Items.GenerateIdNameAsync(serviceProvider, repo.IdNameDescription, "tesztérték", null, cancellationToken));
                await Assert.ThrowsAsync<InvalidOperationException>(() => repository.Items.GenerateIdNameAsync(serviceProvider, repo.IdNameDescription, "tesztérték", new { X = 2 }, cancellationToken));

                var nonSqlGenerator = new IdNameGeneration.IdNameGenerator(Simplifier.Default);
                nextName = await nonSqlGenerator.GenerateAsync(repository.Items, repo.IdNameDescription, "tesztérték", new { ForeignId = 1 }, CancellationToken.None);
                await Assert.ThrowsAsync<ArgumentNullException>(() => nonSqlGenerator.GenerateAsync(repository.Items, repo.IdNameDescription, "tesztérték", null, CancellationToken.None));
                await Assert.ThrowsAsync<InvalidOperationException>(() => nonSqlGenerator.GenerateAsync(repository.Items, repo.IdNameDescription, "tesztérték", new { X = 2 }, CancellationToken.None));
                Assert.Equal("tesztertek-1", nextName);
                nextName = await nonSqlGenerator.GenerateAsync(repository.Items, repo.IdNameDescription, new Item2 { Name = "tesztérték", ForeignId = 1 }, CancellationToken.None);
                Assert.Equal("tesztertek-1", nextName);
            }

            // await repository.PersistAsync(new Item2 { Id = item1.Id, Name = item1.Name, IdName = item1.IdName, ForeignId = item1.ForeignId });
            var item00 = await repository.LookupAsync(item0.Id, cancellationToken);
            Assert.NotNull(item00);
            Assert.Equal(item0.Id, item00.Id);
            Assert.Equal(item0.IdName, item00.IdName);
            var items = await repository.Items.ToListAsync(cancellationToken);
            Assert.Contains(items, i => i.Id == item0.Id && i.IdName == item0.IdName);
            Assert.Contains(items, i => i.Id == item1.Id && i.IdName == item1.IdName);
            Assert.Equal(2, items.Count());
        });

        public virtual Task InsertTwoWithForeign() => ScopedAsync(async (serviceProvider, cancellationToken) =>
        {
            var repository = serviceProvider.GetRequiredService<IDataRepository<Item2, int>>();
            for (var i = 1; i <= 2; ++i)
            {
                await repository.Context.TransactedAsync(IsolationLevel.Serializable, async () =>
                {
                    var item0 = await repository.PersistAsync(new Item2 { Name = "tesztérték", ForeignId = i });
                    var item1 = await repository.PersistAsync(new Item2 { Name = "tesztérték", ForeignId = i });
                    Assert.NotNull(item0.IdName);
                    Assert.Equal("tesztertek", item0.IdName);
                    Assert.NotNull(item1.IdName);
                    Assert.Equal("tesztertek-1", item1.IdName);
                }, cancellationToken);
            }
        });

        public virtual void InsertOneSync() => Scoped(serviceProvider =>
        {
            var repository = serviceProvider.GetRequiredService<IDataRepository<Item, int>>();
            var item = repository.Persist(new Item { Name = "tesztérték" });
            Assert.NotNull(item.IdName);
            Assert.Equal("tesztertek", item.IdName);
            if (repository is EntityFrameworkCore.DataRepository repo)
            {
                var nextName = repository.Context.Transacted(
                    IsolationLevel.Serializable,
                    () => repository.Items.GenerateIdName(serviceProvider, repo.IdNameDescription, "tesztérték", null)
                );
                Assert.Equal("tesztertek-1", nextName);
                var nonSqlGenerator = new IdNameGeneration.IdNameGenerator(Simplifier.Default);
                nextName = nonSqlGenerator.GenerateAsync(repository.Items, repo.IdNameDescription, "tesztérték", null, CancellationToken.None).Result;
                Assert.Equal("tesztertek-1", nextName);
                nextName = nonSqlGenerator.GenerateAsync(repository.Items, repo.IdNameDescription, new Item { Name = "tesztérték" }, CancellationToken.None).Result;
                Assert.Equal("tesztertek-1", nextName);
            }
            // LOOKUP CHECK
            var item0 = repository.Lookup(item.Id);
            Assert.Equal(item.IdName, item0.IdName);
            // UPDATE CHECK
            Item item1 = null;
            repository.Context.Transacted(IsolationLevel.Serializable, () =>
            {
                item.Name = "új érték";
                item1 = repository.Persist(item);
            });
            Assert.NotNull(item1);
            // IMPLICIT ATTACH
            using (var tx = repository.Context.BeginTransaction(IsolationLevel.Serializable))
            {
                var itemX = repository.Persist(new Item { Id = item1.Id, Name = item1.Name, IdName = item1.IdName });
                Assert.Equal("tesztertek", item1.IdName);
                tx.Commit();
                Assert.Throws<InvalidOperationException>(() => tx.Commit());
                tx.Dispose();
            }
            // LIST CHECK
            var items = repository.Items.ToList();
            Assert.True(1 == items.Count);
            Assert.Single(items, i => i.IdName == "tesztertek");
            Assert.Equal(1, repository.Items.Count());
            // DELETE CHECK
            repository.Remove(item1, true);
            items = repository.Items.ToList();
            Assert.Empty(items);
            // DETATCHED DELETE
            Assert.Throws<InvalidOperationException>(() => repository.Remove(new Item { Id = 20, Name = "érték" }, true));
        });
    }
}
