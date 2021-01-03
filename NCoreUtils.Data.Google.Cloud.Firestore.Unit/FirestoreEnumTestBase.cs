using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NCoreUtils.Data.Build;
using NCoreUtils.Linq;
using Xunit;

namespace NCoreUtils.Data.Google.Cloud.Firestore.Unit
{
    public abstract class FirestoreEnumTestBase : TestBase
    {
        public enum SomeEnum
        {
            SomeValue = 0,
            OtherValue = 1
        }

        [Flags]
        public enum SomeFlags
        {
            SomeFlag = 0x01,
            OtherFlag = 0x02
        }

        public class EnumItem : IHasId<string>
        {
            public string Id { get; }

            public SomeEnum Value { get; }

            public EnumItem(string id, SomeEnum value)
            {
                Id = id;
                Value = value;
            }
        }

        public class FlagsItem : IHasId<string>
        {
            public string Id { get; }

            public SomeFlags Value { get; }

            public FlagsItem(string id, SomeFlags value)
            {
                Id = id;
                Value = value;
            }
        }

        protected static void BuildModel(DataModelBuilder builder)
        {
            builder.Entity<EnumItem>(b =>
            {
                b.SetName("enums");
                b.SetKey(e => e.Id);
            });
            builder.Entity<FlagsItem>(b =>
            {
                b.SetName("flags");
                b.SetKey(e => e.Id);
            });
        }

        protected static Action<FirestoreConfiguration> ConfigureFirestore(FirestoreEnumHandling enumHandling)
        {
            return (FirestoreConfiguration configuration) =>
                configuration.ConversionOptions = FirestoreConversionOptionsBuilder.FromOptions(configuration.ConversionOptions ?? FirestoreConversionOptions.Default)
                    .SetEnumHandling(enumHandling)
                    .ToOptions();
        }

        private readonly FirestoreEnumHandling _enumHandling;

        protected FirestoreEnumTestBase(FirestoreEnumHandling enumHandling, Action<DataModelBuilder> buildModel, Action<FirestoreConfiguration>? configure = null)
            : base(buildModel, configure)
        {
            _enumHandling = enumHandling;
        }

        public virtual Task StoreAndRead() => Scoped(async serviceProvider =>
        {
            var erepo = serviceProvider.GetRequiredService<IDataRepository<EnumItem, string>>();
            foreach (var evalue in new [] { SomeEnum.SomeValue, SomeEnum.OtherValue })
            {
                var e0 = await erepo.PersistAsync(new EnumItem(default!, evalue));
                Assert.NotNull(e0);
                var e1 = await erepo.LookupAsync(e0.Id);
                Assert.NotNull(e1);
                Assert.Equal(evalue, e0.Value);
                Assert.Equal(evalue, e1.Value);

                var items = await erepo.Items.Where(e => e.Value == evalue).ToListAsync(default);
                Assert.NotEmpty(items);
            }

            var frepo = serviceProvider.GetRequiredService<IDataRepository<FlagsItem, string>>();
            foreach (var fvalue in new [] { SomeFlags.SomeFlag, SomeFlags.OtherFlag, SomeFlags.SomeFlag|SomeFlags.OtherFlag })
            {
                var e0 = await frepo.PersistAsync(new FlagsItem(default!, fvalue));
                Assert.NotNull(e0);
                var e1 = await frepo.LookupAsync(e0.Id);
                Assert.NotNull(e1);
                Assert.Equal(fvalue, e0.Value);
                Assert.Equal(fvalue, e1.Value);
            }
            if (_enumHandling == FirestoreEnumHandling.AsNumberOrNumberArray || _enumHandling == FirestoreEnumHandling.AsStringOrStringArray)
            {
                var items = await frepo.Items.Where(e => e.Value.HasFlag(SomeFlags.SomeFlag)).ToListAsync(default);
                Assert.True(items.Count >= 2);
                items = await frepo.Items.Where(e => e.Value.HasFlag(SomeFlags.OtherFlag)).ToListAsync(default);
                Assert.True(items.Count >= 2);
            }
            else
            {
                await Assert.ThrowsAsync<InvalidOperationException>(() => frepo.Items.Where(e => e.Value.HasFlag(SomeFlags.SomeFlag)).ToListAsync(default));
            }
        });
    }
}