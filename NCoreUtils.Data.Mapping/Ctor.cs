using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace NCoreUtils.Data
{
    public abstract partial class Ctor : IEquatable<Ctor>
    {
        public ConstructorInfo Constructor { get; }

        public IReadOnlyList<PropertyMapping> Properties { get; }

        public Type Type => Constructor.DeclaringType ?? throw new InvalidOperationException("Unable to get constructor declaring type.");

        internal Ctor(ConstructorInfo constructor, IReadOnlyList<PropertyMapping> properties)
        {
            Constructor = constructor ?? throw new ArgumentNullException(nameof(constructor));
            Properties = properties ?? throw new ArgumentNullException(nameof(properties));
        }

        public CtorExpression CreateExpression(IEnumerable<Expression> arguments)
            => new(this, arguments);

        public object Instantiate(IEnumerable values)
        {
            var arguments = new object[Constructor.GetParameters().Length];
            var valueEnumerator = values.GetEnumerator();
            foreach (var mapping in Properties)
            {
                if (mapping is PropertyMapping.ByCtorParameterMapping ctorParameterMapping)
                {
                    if (!valueEnumerator.MoveNext())
                    {
                        throw new ArgumentException($"No value provided for {mapping}.", nameof(values));
                    }
                    arguments[ctorParameterMapping.By.Position] = valueEnumerator.Current;
                }
            }
            var obj = Constructor.Invoke(arguments);
            foreach (var mapping in Properties)
            {
                if (mapping is PropertyMapping.BySetterMapping propertyMapping)
                {
                    if (!valueEnumerator.MoveNext())
                    {
                        throw new ArgumentException($"No value provided for {mapping}.", nameof(values));
                    }
                    propertyMapping.TargetProperty.SetValue(obj, valueEnumerator.Current, null);
                }
            }
            return obj;
        }

        public bool Equals(Ctor? other)
            => other != null
                && Constructor.Equals(other.Constructor)
                && Properties.SequenceEqual(other.Properties);

        public override bool Equals(object? obj)
            => obj is Ctor other && Equals(other);

        public override int GetHashCode()
        {
            var hash = new HashCode();
            hash.Add(Constructor);
            hash.Add(Properties.Count);
            foreach (var prop in Properties)
            {
                hash.Add(prop);
            }
            return hash.ToHashCode();
        }
    }

    public sealed class Ctor<T> : Ctor
    {
        internal Ctor(ConstructorInfo constructor, IReadOnlyList<PropertyMapping> properties)
            : base(constructor, properties)
        { }
    }
}