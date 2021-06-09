
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NCoreUtils.Data.Build;
using NCoreUtils.Linq;
using Xunit;

namespace NCoreUtils.Data.Google.Cloud.Firestore.Unit
{
    public class FirestoreMultiqueryTests : TestBase
    {
        private static void BuildModel(DataModelBuilder builder)
        {
            builder.Entity<ItemWithArray>(b =>
            {
                b.SetKey(new [] { b.Property(e => e.Id).Property });
            });
        }

        public FirestoreMultiqueryTests() : base(BuildModel) { }

        [Fact]
        public Task SelectPrimitive() => Scoped(async serviceProvider =>
        {
            var name0 = "name0";
            var name1 = "name1";
            var name2 = "name2";
            var repo = serviceProvider.GetRequiredService<IDataRepository<ItemWithArray, string>>();
            var item0 = await repo.PersistAsync(new ItemWithArray(default!, name0, new string[] { "a", "b" }));
            var item1 = await repo.PersistAsync(new ItemWithArray(default!, name1, new string[] { "b", "c" }));
            var item2 = await repo.PersistAsync(new ItemWithArray(default!, name2, new string[] { "c", "d" }));

            // query all items (multiquery)
            var allLetters = "abcdefghijklmnopqrstuvwxyz".ToCharArray().Select(ch => ch.ToString()).ToArray();
            var items = await repo.Items.Where(e => e.Values.ContainsAny(allLetters)).ToListAsync(default);
            Assert.Equal(3, items.Count);
            Assert.Contains(items, i => i.Name == name0);
            Assert.Contains(items, i => i.Name == name1);
            Assert.Contains(items, i => i.Name == name2);
            // query with single query (single query)
            items = await repo.Items.Where(e => e.Values.ContainsAny(new string[] { "a", "b" })).ToListAsync(default);
            Assert.Equal(2, items.Count);
            Assert.Contains(items, i => i.Name == name0);
            Assert.Contains(items, i => i.Name == name1);
            // query with single query (multiquery)
            var allButCD = "abefghijklmnopqrstuvwxyz".ToCharArray().Select(ch => ch.ToString()).ToArray();
            items = await repo.Items.Where(e => e.Values.ContainsAny(allButCD)).ToListAsync(default);
            Assert.Equal(2, items.Count);
            Assert.Contains(items, i => i.Name == name0);
            Assert.Contains(items, i => i.Name == name1);
            // query first item (mutliquery)
            var item = await repo.Items.Where(e => e.Values.ContainsAny(allLetters)).OrderBy(e => e.Name).FirstOrDefaultAsync(default);
            Assert.NotNull(item);
            Assert.Equal(name0, item.Name);
            item = await repo.Items.Where(e => e.Values.ContainsAny(allLetters)).OrderByDescending(e => e.Name).FirstOrDefaultAsync(default);
            Assert.NotNull(item);
            Assert.Equal(name2, item.Name);
            item = await repo.Items.Where(e => e.Values.ContainsAny(allLetters)).OrderBy(e => e.Name).FirstAsync(default);
            Assert.NotNull(item);
            Assert.Equal(name0, item.Name);
            item = await repo.Items.Where(e => e.Values.ContainsAny(allLetters)).OrderByDescending(e => e.Name).FirstAsync(default);
            Assert.NotNull(item);
            Assert.Equal(name2, item.Name);


            await repo.RemoveAsync(item0, true);
            await repo.RemoveAsync(item1, true);
            await repo.RemoveAsync(item2, true);
        });
    }
}