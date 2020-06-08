using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NCoreUtils.Data.Build;
using NCoreUtils.Linq;
using Xunit;

namespace NCoreUtils.Data.Google.Cloud.Firestore.Unit
{
    public class FirestoreSelectorTests : TestBase
    {
        private static void BuildModel(DataModelBuilder builder)
        {
            builder.Entity<SimpleItem>(b =>
            {
                b.SetKey(new [] { b.Property(e => e.Id).Property });
            });
        }

        public FirestoreSelectorTests() : base(BuildModel) { }

        [Fact]
        public Task SelectPrimitive() => Scoped(async serviceProvider =>
        {
            var repo = serviceProvider.GetRequiredService<IDataRepository<SimpleItem, string>>();
            var now = RoundToMilliseconds(DateTimeOffset.Now);
            var item0 = new SimpleItem(default!, "string", 1, 1.0, true, now);
            var item = await repo.PersistAsync(item0);
            var svalue = await repo.Items.Where(e => e.Id == item.Id).Select(e => e.StringValue).FirstOrDefaultAsync(CancellationToken.None);
            Assert.Equal(item0.StringValue, svalue);
            svalue = (await repo.Items.Where(e => e.Id == item.Id).Select(e => e.StringValue).ToListAsync(CancellationToken.None)).FirstOrDefault();
            Assert.Equal(item0.StringValue, svalue);
            await repo.RemoveAsync(item, true);
        });
    }
}