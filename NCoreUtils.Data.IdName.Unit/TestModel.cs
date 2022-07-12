using System.Linq;
using NCoreUtils.Data.Build;
using NCoreUtils.Data.Model;
using Xunit;

namespace NCoreUtils.Data
{
    public class TestModel
    {
        [Fact]
        public void Basic()
        {
            var builder = new DataModelBuilder();
            DataEntityBuilder entityBuilder = default!;
            builder.Entity<Item>(b =>
            {
                entityBuilder = b;
                b.SetName("ename");
                foreach (var p in b.Properties)
                {
                    p.Value.SetName("name_" + p.Key.Name);
                }
                b.Property(e => e.Name)
                    .SetDefaultValue("defname")
                    .SetMaxLength(300)
                    .SetMinLength(300)
                    .SetRequired(true)
                    .SetUnicode(true);
            });
            builder.Entity(typeof(Item), b =>
            {
                Assert.Same(b, entityBuilder);
            });
            var model = new DataModel(builder);
            Assert.Single(model.Entities);
            var e = model.Entities[0];
            Assert.Equal("ename", e.Name);
            foreach (var p in e.Properties)
            {
                Assert.Equal(p.Name, "name_" + p.Property.Name);
                if (p.Property.Name == nameof(Item.Name))
                {
                    Assert.True(p.TryGetDefaultValue(out var def));
                    var sdef = Assert.IsType<string>(def);
                    Assert.Equal("defname", sdef);
                    Assert.Equal(300, p.MaxLength);
                    Assert.Equal(300, p.MinLength);
                    Assert.True(p.Required);
                    Assert.True(p.Unicode);
                }
                else
                {
                    Assert.False(p.TryGetDefaultValue(out var def));
                    Assert.Null(p.MaxLength);
                    Assert.Null(p.MinLength);
                    Assert.Null(p.Required);
                    Assert.Null(p.Unicode);
                }
            }
        }
    }
}