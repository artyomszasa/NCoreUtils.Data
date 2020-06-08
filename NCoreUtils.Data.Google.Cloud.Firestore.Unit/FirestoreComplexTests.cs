using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NCoreUtils.Data.Build;
using Xunit;

namespace NCoreUtils.Data.Google.Cloud.Firestore.Unit
{
    public class FirestoreComplexTests : TestBase
    {
        private static void BuildModel(DataModelBuilder builder)
        {
            builder.Entity<ComplexItem>(b =>
            {
                b.SetKey(new [] { b.Property(e => e.Id).Property });
            });
            builder.Entity<SimpleItem>();
        }

        public FirestoreComplexTests() : base(BuildModel) { }

        [Fact]
        public Task SelectComplex() => Scoped(async serviceProvider =>
        {
            var repo = serviceProvider.GetRequiredService<IDataRepository<ComplexItem, string>>();
            var now = RoundToMilliseconds(DateTimeOffset.Now);
            var guid = Guid.NewGuid();
            var item0 = new ComplexItem(
                default!,
                2.12m,
                0.5f,
                guid,
                new SimpleItem("subitem", "string", 1, 1.0, false, now),
                new SimpleItem[]
                {
                    new SimpleItem("subitem0", "string", 1, 1.0, true, now),
                    new SimpleItem("subitem1", "string", 1, 1.0, true, now),
                    new SimpleItem("subitem2", "string", 1, 1.0, true, now)
                }
            );
            var item = await repo.PersistAsync(item0);
            Assert.NotNull(item);
            Assert.NotNull(item.Id);
            Assert.Equal(2.12m, item.Decimal);
            Assert.Equal(0.5f, item.Float);
            Assert.Equal(guid, item.Guid);
            Assert.NotNull(item.Subitem);
            var subitem = item.Subitem;
            Assert.Equal("subitem", subitem.Id);
            Assert.Equal("string", subitem.StringValue);
            Assert.Equal(1, subitem.NumValue);
            Assert.Equal(1.0, subitem.FloatValue);
            Assert.False(subitem.BooleanValue);
            Assert.Equal(now, subitem.DateValue);
            Assert.NotNull(item.Collection);
            Assert.Equal(3, item.Collection.Count);
            for (var i = 0; i < 3; ++i)
            {
                subitem = item.Collection[i];
                Assert.Equal($"subitem{i}", subitem.Id);
                Assert.Equal("string", subitem.StringValue);
                Assert.Equal(1, subitem.NumValue);
                Assert.Equal(1.0, subitem.FloatValue);
                Assert.True(subitem.BooleanValue);
                Assert.Equal(now, subitem.DateValue);
            }
        });
    }
}