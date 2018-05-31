using System;
using System.Collections.Concurrent;
using System.Linq.Expressions;

namespace NCoreUtils.Linq
{
    abstract class SpliceValueExpression : SpliceExpression
    {
        abstract class Factory
        {
            static readonly ConcurrentDictionary<Type, Factory> _cache = new ConcurrentDictionary<Type, Factory>();

            public static SpliceValueExpression Create(Type type, string name)
                => _cache.GetOrAdd(type, ty => (Factory)Activator.CreateInstance(typeof(Factory<>).MakeGenericType(ty), true))
                    .Create(name);

            protected abstract SpliceValueExpression Create(string name);
        }

        sealed class Factory<T> : Factory
        {
            protected override SpliceValueExpression Create(string name) => new SpliceValueExpression<T>(name);
        }

        public static SpliceValueExpression Create(Type type, string name) => Factory.Create(type, name);

        protected SpliceValueExpression(string name) : base(name) { }
    }

    class SpliceValueExpression<T> : SpliceValueExpression
    {
        public override Type Type => typeof(T);

        public SpliceValueExpression(string name) : base(name) { }
    }
}