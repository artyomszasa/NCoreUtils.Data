using System;
using System.Linq.Expressions;
using Xunit;

namespace NCoreUtils.Data.Unit
{
    public class TestPrimitives
    {
        public class RealId : IHasId<int>
        {
            [TargetProperty(nameof(Uid))]
            int IHasId<int>.Id => Uid;
            public int Uid { get; }
        }

        [Fact]
        public void IdValidation()
        {
            Assert.True(IdUtils.IsValidId(1));
            Assert.True(IdUtils.IsValidId((sbyte)1));
            Assert.True(IdUtils.IsValidId((short)1));
            Assert.True(IdUtils.IsValidId((long)1));
            Assert.True(IdUtils.IsValidId("0"));

            Assert.False(IdUtils.IsValidId(-1));
            Assert.False(IdUtils.IsValidId((sbyte)-1));
            Assert.False(IdUtils.IsValidId((short)-1));
            Assert.False(IdUtils.IsValidId((long)-1));
            Assert.False(IdUtils.IsValidId((string)null));
        }

        [Fact]
        public void TryGetId()
        {
            Assert.True(IdUtils.TryGetIdProperty(typeof(Item), out var prop));
            Assert.Equal(typeof(int), prop.PropertyType);
            Assert.False(IdUtils.TryGetIdProperty(typeof(int), out prop));
            Assert.Null(prop);
        }

        [Fact]
        public void TryGetRealId()
        {
            Assert.True(IdUtils.TryGetRealIdProperty(typeof(Item), out var prop));
            Assert.Equal(typeof(int), prop.PropertyType);
            Assert.Equal("Id", prop.Name);
            Assert.True(IdUtils.TryGetRealIdProperty(typeof(RealId), out prop));
            Assert.Equal("Uid", prop.Name);
            Assert.False(IdUtils.TryGetRealIdProperty(typeof(int), out prop));
            Assert.Null(prop);
        }

        Expression<Func<T, int>> CreateExpression<T>() where T : IHasId<int>
        {
            return e => e.Id;
        }

        [Fact]
        public void ExplicitProperties()
        {
            var expr = LinqExtensions.ReplaceExplicitProperties(CreateExpression<RealId>());
            Assert.Equal(nameof(RealId.Uid), expr.ExtractProperty().Name);
        }

    }
}