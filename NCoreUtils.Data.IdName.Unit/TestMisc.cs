using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using NCoreUtils.Data.Events;
using NCoreUtils.Data.IdNameGeneration;
using NCoreUtils.Text;
using Xunit;

namespace NCoreUtils.Data.Unit
{
    public class TestMisc
    {
        class Box<T>
        {
            public T Value { get; set; }

            public Box(T value) => Value = value;

            public Box() { }
        }

        class HasField
        {
            public int Field = 0;
        }

        class HasId<T> : IHasId<T>
        {
            public T Id { get; set; }
        }

        class CtorOnly
        {
            public int Value { get; }

            public CtorOnly(int value) => Value = value;
        }

        class DataEventHandler : IDataEventHandler
        {
            readonly List<IDataEvent> _events = new List<IDataEvent>();

            public IReadOnlyList<IDataEvent> Events => _events;

            public ValueTask HandleAsync(IDataEvent @event, CancellationToken cancellationToken = default)
            {
                _events.Add(@event);
                return default;
            }
        }

        DbContext MockDbContext(string providerName)
        {
            var dbContextBox = new Box<DbContext>();
            var mockDbContext = new Mock<DbContext>(MockBehavior.Strict);
            mockDbContext.SetupGet(e => e.Database).Returns(() =>
            {
                var mockDb = new Mock<DatabaseFacade>(MockBehavior.Strict, dbContextBox.Value);
                mockDb.SetupGet(e => e.ProviderName).Returns(providerName);
                return mockDb.Object;
            });
            var dbContext = mockDbContext.Object;
            dbContextBox.Value = dbContext;
            return dbContext;
        }

        [Fact]
        public void TestBuiltInStoredProcedureGenerators()
        {
            Assert.False(BuiltInStoredProcedureGenerators.TryGetGenerator("NoNamespace.NoClass", out var _));
        }

        [Fact]
        public void TestIdNameGenerationInitialization()
        {
            Assert.Throws<ArgumentNullException>(() => new IdNameGenerationInitialization(null, null));
            Assert.Throws<InvalidOperationException>(() => new IdNameGenerationInitialization(MockDbContext("NoNamespace.NoClass"), null));
        }

        [Fact]
        public void TestSqlIdNameGenerator()
        {
            var dbContext = MockDbContext("Npgsql.EntityFrameworkCore.PostgreSQL");
            var initialization = new IdNameGenerationInitialization(dbContext, null);
            var simplifier = Simplifier.Default;

            var err = Assert.Throws<ArgumentNullException>(() => new SqlIdNameGenerator(null, dbContext, simplifier));
            Assert.Equal("initialization", err.ParamName);
            err = Assert.Throws<ArgumentNullException>(() => new SqlIdNameGenerator(initialization, null, simplifier));
            Assert.Equal("dbContext", err.ParamName);
            err = Assert.Throws<ArgumentNullException>(() => new SqlIdNameGenerator(initialization, dbContext, null));
            Assert.Equal("simplifier", err.ParamName);
        }


        [Fact]
        public void TestIdUtils()
        {
            Assert.True(new HasId<short>{ Id = 2 }.HasValidId());
            Assert.True(new HasId<int>{ Id = 2 }.HasValidId());
            Assert.True(new HasId<long>{ Id = 2 }.HasValidId());
            Assert.True(new HasId<string>{ Id = "xxx" }.HasValidId());
            Assert.False(new HasId<short>().HasValidId());
            Assert.False(new HasId<int>().HasValidId());
            Assert.False(new HasId<long>().HasValidId());
            Assert.False(new HasId<string>().HasValidId());

            Assert.True(IdUtils.TryGetIdType(typeof(IHasId<int>), out var idType));
            Assert.Equal(typeof(int), idType);

            var predicate = IdUtils.CreateIdEqualsPredicate<HasId<int>, int>(2).Compile();
            Assert.True(predicate(new HasId<int> { Id = 2 }));
            Assert.False(predicate(new HasId<int> { Id = 3 }));

        }

        [Fact]
        public void TestExpressionExtensions()
        {
            Expression<Func<HasField, int>> expr0 = x => x.Field;
            Expression<Func<int, int>> expr1 = x => x;
            Expression<Func<HasId<short>, int>> expr2 = x => (int)x.Id;
            Expression<Func<Item, object>> expr3 = x => new { x.Id, x.IdName };
            Expression<Func<Item, CtorOnly>> expr4 = x => new CtorOnly(x.Id);
            Assert.Throws<ArgumentNullException>(() => ExpressionExtensions.MaybeExtractProperty(null));
            Assert.False(expr0.MaybeExtractProperty().HasValue);
            Assert.False(expr1.MaybeExtractProperty().HasValue);
            Assert.True(expr2.MaybeExtractProperty().HasValue);
            Assert.Throws<InvalidOperationException>(() => expr0.ExtractProperty());
            Assert.Throws<InvalidOperationException>(() => expr1.ExtractProperty());
            Assert.Throws<InvalidOperationException>(() => expr1.ExtractProperties().ToList());
            Assert.Throws<ArgumentNullException>(() => ExpressionExtensions.MaybeExtractQueryable(null));
            Assert.Throws<ArgumentNullException>(() => ExpressionExtensions.ExtractProperties(null).ToList());
            Assert.Equal(new [] { typeof(HasId<short>).GetProperty(nameof(HasId<short>.Id)) }, expr2.ExtractProperties().ToArray());
            Assert.Empty(expr1.ExtractProperties(false).ToList());
            Assert.Equal(new HashSet<PropertyInfo>(new [] { typeof(Item).GetProperty(nameof(Item.Id)), typeof(Item).GetProperty(nameof(Item.IdName)) }), new HashSet<PropertyInfo>(expr3.ExtractProperties()));
            Assert.Empty(expr4.ExtractProperties(false).ToList());
            Assert.Throws<InvalidOperationException>(() => expr4.ExtractProperties().ToList());
        }

        [Fact]
        public void TestDummyStringDecomposition()
        {
            string input = null;
            Assert.Throws<ArgumentNullException>(() => (DummyStringDecomposition)input);
        }

        [Fact]
        public void TestFileStringDecomposition()
        {
            {
                string input = "file.ext";
                Assert.Throws<ArgumentNullException>(() => new FileNameDecomposition(null));
                var decomposition = new FileNameDecomposition(input);
                Assert.Equal(".ext", decomposition.Extension);
                Assert.Equal("file", decomposition.MainPart);
                Assert.Equal(input, decomposition.Rebuild(decomposition.MainPart, null));
                Assert.Equal(input, decomposition.Rebuild(decomposition.MainPart, string.Empty));
                Assert.Equal("file-1.ext", decomposition.Rebuild(decomposition.MainPart, "1"));
            }
            // DECOMPOSER
            {
                string input = "file.ext";
                Assert.Throws<ArgumentNullException>(() => FileNameDecomposition.Decomposer.Decompose(null));
                var decomposition = Assert.IsType<FileNameDecomposition>(FileNameDecomposition.Decomposer.Decompose(input));
                Assert.Equal(".ext", decomposition.Extension);
                Assert.Equal("file", decomposition.MainPart);
                Assert.Equal(input, decomposition.Rebuild(decomposition.MainPart, null));
                Assert.Equal(input, decomposition.Rebuild(decomposition.MainPart, string.Empty));
                Assert.Equal("file-1.ext", decomposition.Rebuild(decomposition.MainPart, "1"));
            }
            // NO EXT
            {
                string input = "filenoext";
                Assert.Throws<ArgumentNullException>(() => new FileNameDecomposition(null));
                var decomposition = new FileNameDecomposition(input);
                Assert.Null(decomposition.Extension);
                Assert.Equal(input, decomposition.MainPart);
                Assert.Equal(input, decomposition.Rebuild(decomposition.MainPart, null));
                Assert.Equal(input, decomposition.Rebuild(decomposition.MainPart, string.Empty));
                Assert.Equal("filenoext-1", decomposition.Rebuild(decomposition.MainPart, "1"));
            }
        }

        [Fact]
        public void TestIdNameDescriptionBuilder()
        {
            var builder = new IdNameDescriptionBuilder<Item>();
            builder.SetIdNameProperty(i => i.IdName);
            Assert.Equal(typeof(Item).GetProperty(nameof(Item.IdName)), builder.IdNameProperty);
            builder.AddAdditionalIndexProperties(new [] { typeof(Item).GetProperty(nameof(Item.IdName)) });
            Assert.All(builder.AdditionalIndexProperties, p => p.Equals(typeof(Item).GetProperty(nameof(Item.IdName))));
            Assert.Throws<ArgumentNullException>("properties", () => builder.AddAdditionalIndexProperties(null));
        }

        [Fact]
        public void TestDataEventFilter()
        {
            var item = new Item();
            IDataRepository<Item> repository = new Mock<IDataRepository<Item>>().Object;
            var realHandler = new DataEventHandler();
            Assert.Throws<ArgumentNullException>(() => DataEventFilter.Filter(null, (e, _) => new ValueTask<bool>(e.Operation == DataOperation.Insert)));
            Assert.Throws<ArgumentNullException>(() => DataEventFilter.Filter(realHandler, null));
            var handler = DataEventFilter.Filter(realHandler, (e, _) =>
            {
                switch (e)
                {
                    case DataInsertEvent<Item> i:
                        Assert.Equal(typeof(Item), i.EntityType);
                        Assert.Same(repository, i.Repository);
                        Assert.Same(item, i.Entity);
                        break;
                    case DataDeleteEvent<Item> d:
                        Assert.Equal(typeof(Item), d.EntityType);
                        Assert.Same(repository, d.Repository);
                        Assert.Same(item, d.Entity);
                        break;
                    case DataUpdateEvent<Item> u:
                        Assert.Equal(typeof(Item), u.EntityType);
                        Assert.Same(repository, u.Repository);
                        Assert.Same(item, u.Entity);
                        break;
                }
                Assert.Same(item, e.Entity);
                return new ValueTask<bool>(e.Operation == DataOperation.Insert);
            });
            var serviceProvider = new ServiceCollection().BuildServiceProvider();
            var insertEvent = new DataInsertEvent<Item>(serviceProvider, repository, item);
            var updateEvent = new DataUpdateEvent<Item>(serviceProvider, repository, item);
            var deleteEvent = new DataDeleteEvent<Item>(serviceProvider, repository, item);
            handler.HandleAsync(insertEvent).AsTask().Wait();
            handler.HandleAsync(updateEvent).AsTask().Wait();
            handler.HandleAsync(deleteEvent).AsTask().Wait();
            var handledCount = realHandler.Events.Count;
            Assert.Equal(1, handledCount);
            Assert.Same(insertEvent, realHandler.Events[0]);
        }

        [Fact]
        public void TestDataRepositoryExtensions()
        {
            Assert.Throws<ArgumentNullException>(() => DataRepositoryExtensions.Lookup<Item, int>(null, 0));
            Assert.Throws<ArgumentNullException>(() => DataRepositoryExtensions.Persist(null, new Item()));
            Assert.Throws<ArgumentNullException>(() => DataRepositoryExtensions.Remove(null, new Item()));
        }

        [Fact]
        public void TestDataRepositoryContextExtensions()
        {
            var context = new Mock<IDataRepositoryContext>().Object;
            Assert.Throws<ArgumentNullException>(() => DataRepositoryContextExtensions.BeginTransaction(null, IsolationLevel.Serializable));
            Assert.Throws<ArgumentNullException>(() => DataRepositoryContextExtensions.Transacted(null, IsolationLevel.Serializable, () => {}));
            Assert.Throws<ArgumentNullException>(() => DataRepositoryContextExtensions.Transacted(null, IsolationLevel.Serializable, () => 2));
            Assert.Throws<ArgumentNullException>(() => DataRepositoryContextExtensions.Transacted(context, IsolationLevel.Serializable, null));
            Assert.Throws<ArgumentNullException>(() => DataRepositoryContextExtensions.Transacted(context, IsolationLevel.Serializable, (Func<int>)null));
            Assert.ThrowsAsync<ArgumentNullException>(() => DataRepositoryContextExtensions.TransactedAsync(null, IsolationLevel.Serializable, () => Task.CompletedTask));
            Assert.ThrowsAsync<ArgumentNullException>(() => DataRepositoryContextExtensions.TransactedAsync(null, IsolationLevel.Serializable, () => Task.FromResult(2)));
            Assert.ThrowsAsync<ArgumentNullException>(() => DataRepositoryContextExtensions.TransactedAsync(context, IsolationLevel.Serializable, null));
            Assert.ThrowsAsync<ArgumentNullException>(() => DataRepositoryContextExtensions.TransactedAsync(context, IsolationLevel.Serializable, (Func<Task<int>>)null));
        }
    }
}