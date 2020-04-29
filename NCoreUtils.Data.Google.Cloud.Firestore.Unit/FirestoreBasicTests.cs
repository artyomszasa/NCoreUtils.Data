using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NCoreUtils.Linq;
using Xunit;

namespace NCoreUtils.Data.Google.Cloud.Firestore.Unit
{
    public class FirestoreBasicTests : TestBase
    {
        [Fact]
        public Task PersistAndQuery() => Scoped(async serviceProvider =>
        {
            var repo = serviceProvider.GetRequiredService<IDataRepository<SimpleItem, string>>();
            var now = RoundToMilliseconds(DateTimeOffset.Now);
            var item0 = new SimpleItem(default!, "string", 1, 1.0, true, now);
            var item = await repo.PersistAsync(item0);
            Assert.NotNull(item);
            Assert.NotNull(item.Id);
            Assert.Equal(item0.StringValue, item.StringValue);
            Assert.Equal(item0.NumValue, item.NumValue);
            Assert.Equal(item0.FloatValue, item.FloatValue);
            Assert.Equal(item0.BooleanValue, item.BooleanValue);
            Assert.Equal(item0.DateValue, item.DateValue);
            await repo.RemoveAsync(item, true);
            item = await repo.LookupAsync(item.Id);
            Assert.Null(item);
        });

        [Fact]
        public Task QueryByKey() => Scoped(async serviceProvider =>
        {
            var repo = serviceProvider.GetRequiredService<IDataRepository<SimpleItem, string>>();
            var now = RoundToMilliseconds(DateTimeOffset.Now);
            var item0 = new SimpleItem(default!, "string", 1, 1.0, true, now);
            var item = await repo.PersistAsync(item0);
            Assert.NotNull(item);
            Assert.NotNull(item.Id);
            var id = item.Id;
            var items = await repo.Items.Where(e => e.Id == id).ToListAsync(CancellationToken.None);
            Assert.Single(items);
            Assert.Equal(id, items[0].Id);
            await repo.RemoveAsync(items[0], true);
        });
    }
}
