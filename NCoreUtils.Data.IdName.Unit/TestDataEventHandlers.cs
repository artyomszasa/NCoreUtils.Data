using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NCoreUtils.Data.Events;
using NCoreUtils.Linq;
using Xunit;

namespace NCoreUtils.Data
{
    public class TestDataEvventHandlers
    {
        private class DummyEventHandler : IDataEventHandler
        {
            public int Uid { get; }

            public DummyEventHandler(int uid)
                => Uid = uid;

            public ValueTask HandleAsync(IDataEvent @event, CancellationToken cancellationToken = default) => throw new System.NotImplementedException();
        }

        private sealed class OpCollector : List<(DataOperation Operation, object Entity)> { }

        private sealed class TestObserver : DataEventObserver
        {
            private readonly OpCollector _collector;

            public TestObserver(OpCollector collector)
            {
                _collector = collector;
            }

            protected override ValueTask HandleAsync(DataOperation operation, object entity, CancellationToken cancellationToken)
            {
                _collector.Add((operation, entity));
                return default;
            }
        }

        [Fact]
        public void Basic()
        {
            var handles = new DataEventHandlers();
            var h0 = new DummyEventHandler(0);
            var h1 = new DummyEventHandler(1);
            var h2 = new DummyEventHandler(2);

            Assert.Equal(0, handles.Count);

            handles.Add(h0);
            Assert.Equal(1, handles.Count);
            Assert.Equal(h0.Uid, Assert.IsType<DummyEventHandler>(handles[0]).Uid);

            handles.Add(h1);
            Assert.Equal(2, handles.Count);
            Assert.Equal(h0.Uid, Assert.IsType<DummyEventHandler>(handles[0]).Uid);
            Assert.Equal(h1.Uid, Assert.IsType<DummyEventHandler>(handles[1]).Uid);

            handles.Insert(1, h2);
            Assert.Equal(3, handles.Count);
            Assert.Equal(h0.Uid, Assert.IsType<DummyEventHandler>(handles[0]).Uid);
            Assert.Equal(h2.Uid, Assert.IsType<DummyEventHandler>(handles[1]).Uid);
            Assert.Equal(h1.Uid, Assert.IsType<DummyEventHandler>(handles[2]).Uid);

            handles.Remove(h0);
            Assert.Equal(2, handles.Count);
            Assert.Equal(h2.Uid, Assert.IsType<DummyEventHandler>(handles[0]).Uid);
            Assert.Equal(h1.Uid, Assert.IsType<DummyEventHandler>(handles[1]).Uid);

            handles[0] = h0;
            Assert.Equal(2, handles.Count);
            Assert.Equal(h0.Uid, Assert.IsType<DummyEventHandler>(handles[0]).Uid);
            Assert.Equal(h1.Uid, Assert.IsType<DummyEventHandler>(handles[1]).Uid);

            handles.RemoveAt(1);
            Assert.Equal(1, handles.Count);
            Assert.Equal(h0.Uid, Assert.IsType<DummyEventHandler>(handles[0]).Uid);

        }

        [Fact]
        public void Observe()
        {
            var collector = new OpCollector();
            var handles = new DataEventHandlers();
            var iitems = new List<Item>();
            var uitems = new List<Item>();
            var ditems = new List<Item>();
            var aiitems = new List<Item>();
            var auitems = new List<Item>();
            var aditems = new List<Item>();
            using var sp = new ServiceCollection()
                .AddLogging(b => b.ClearProviders().SetMinimumLevel(LogLevel.Critical))
                .AddSingleton(collector)
                .AddSingleton<IDataEventHandlers>(handles)
                .AddInMemoryDataRepositoryContext()
                .AddInMemoryDataRepository<Item, int>()
                .BuildServiceProvider(false);
            handles.Observe<TestObserver>();
            handles.ObserveInsert<Item>(iitems.Add);
            handles.ObserveUpdate<Item>(uitems.Add);
            handles.ObserveDelete<Item>(ditems.Add);
            handles.ObserveInsert<Item>((i, _) =>
            {
                aiitems.Add(i);
                return default;
            });
            handles.ObserveUpdate<Item>((i, _) =>
            {
                auitems.Add(i);
                return default;
            });
            handles.ObserveDelete<Item>((i, _) =>
            {
                aditems.Add(i);
                return default;
            });
            var item = new Item();
            // insert
            var repo = sp.GetRequiredService<IDataRepository<Item>>();
            repo.Persist(item);
            var (op, itemx) = Assert.Single(collector);
            Assert.Equal(DataOperation.Insert, op);
            Assert.Same(item, itemx);
            Assert.Single(iitems);
            Assert.Single(aiitems);
            Assert.Same(item, iitems[0]);
            Assert.Same(item, aiitems[0]);
            var itemy = repo.Items.FirstOrDefaultAsync(e => e.Id == item.Id, CancellationToken.None).Result;
            Assert.NotNull(itemy);
            Assert.Same(item, itemy);
            var items = repo.Items.Where(e => e.Id == item.Id).ToListAsync(CancellationToken.None).Result;
            Assert.Single(items);
            Assert.Same(item, items[0]);
            // update
            var uitem = repo.Persist(new Item { Id = item.Id, IdName = "xxxx" });
            Assert.Equal(2, collector.Count);
            var (uop, uitemx) = Assert.Single(collector, e => e.Operation == DataOperation.Update);
            Assert.Same(uitem, uitemx);
            var uitemy = Assert.Single(uitems);
            Assert.Same(uitem, uitemy);
            var auitemy = Assert.Single(auitems);
            Assert.Same(uitem, auitemy);
            // delete
            repo.Remove(uitem);
            Assert.Equal(3, collector.Count);
            var (dop, ditemx) = Assert.Single(collector, e => e.Operation == DataOperation.Update);
            Assert.Same(uitem, ditemx);
            var ditemy = Assert.Single(ditems);
            Assert.Same(uitem, ditemy);
            var aditemy = Assert.Single(aditems);
            Assert.Same(uitem, aditemy);
        }
    }
}