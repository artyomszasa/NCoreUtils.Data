using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace NCoreUtils.Data
{
    public abstract partial class Ctor
    {
        public ConstructorInfo Constructor { get; }

        public IReadOnlyList<PropertyMapping> Properties { get; }

        public Type Type => Constructor.DeclaringType;

        internal Ctor(ConstructorInfo constructor, IReadOnlyList<PropertyMapping> properties)
        {
            Constructor = constructor ?? throw new ArgumentNullException(nameof(constructor));
            Properties = properties ?? throw new ArgumentNullException(nameof(properties));
        }

        public CtorExpression CreateExpression(IEnumerable<Expression> arguments)
            => new CtorExpression(this, arguments);
    }

    public sealed class Ctor<T> : Ctor
    {
        internal Ctor(ConstructorInfo constructor, IReadOnlyList<PropertyMapping> properties)
            : base(constructor, properties)
        { }
    }
}