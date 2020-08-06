using System;
using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace NCoreUtils.Data
{
    public class TestExceptions : TestBase
    {
        private static void InitDbContext(IConfiguration configuration, DbContextOptionsBuilder builder)
        {
            builder.UseInMemoryDatabase("test");
            builder.ConfigureWarnings(w =>
            {
                w.Ignore(InMemoryEventId.TransactionIgnoredWarning);
            });
        }

        public TestExceptions()
            : base(InitDbContext)
        { }

        [Fact]
        public void TwoTransactions() => Scoped(sp =>
        {
            var repository = sp.GetRequiredService<IDataRepository<Item, int>>();
            var context = repository.Context;
            var exn = Assert.Throws<InvalidOperationException>(() =>
            {
                context.Transacted(IsolationLevel.Serializable, () =>
                {
                    using var _ = context.BeginTransaction(IsolationLevel.Serializable);
                });
            });
            Assert.Equal("Transaction has already been started in the actual context.", exn.Message);
        });

        [Fact]
        public void RollbackOnDispose() => Scoped(sp =>
        {
            var repository0 = sp.GetRequiredService<IDataRepository<Item, int>>();
            var repository = sp.GetRequiredService<IDataRepository<Item>>();
            var context0 = sp.GetRequiredService<IDataRepositoryContext>();
            Assert.Same(repository0, repository);
            var context = repository.Context;
            Assert.Same(context0, context);
            var rollbacked = false;
            {
                using var tx = context.BeginTransaction(IsolationLevel.Serializable);
                ((EntityFrameworkCore.DataTransaction)tx).OnRollback += (_, __) => rollbacked = true;
                // NOT: no explicit commit/rollback
            }
            Assert.True(rollbacked);
        });

        [Fact]
        public void RollbackDebounce() => Scoped(sp =>
        {
            var repository = sp.GetRequiredService<IDataRepository<Item>>();
            var context = repository.Context;
            var rollbacked = 0;
            {
                using var tx = context.BeginTransaction(IsolationLevel.Serializable);
                ((EntityFrameworkCore.DataTransaction)tx).OnRollback += (_, __) => ++rollbacked;
                // NOT: no explicit rollback
                tx.Rollback();
                Assert.Throws<InvalidOperationException>(() => tx.Rollback());
            }
            Assert.Equal(1, rollbacked);
        });
    }
}