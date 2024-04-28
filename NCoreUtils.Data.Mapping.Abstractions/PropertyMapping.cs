using System;
using System.Collections.Generic;
using System.Reflection;

namespace NCoreUtils.Data;

public abstract class PropertyMapping : IEquatable<PropertyMapping>, IComparable<PropertyMapping>
{
    private static readonly Comparer<int> _int32Comparer = Comparer<int>.Default;

    private static readonly StringComparer _stringComparer = StringComparer.InvariantCulture;

    public class ByCtorParameterMapping : PropertyMapping
    {
        public ParameterInfo By { get; }

        public override bool IsByCtorParameter => true;

        public override bool IsBySetter => false;

        internal ByCtorParameterMapping(PropertyInfo targetProperty, ParameterInfo by)
            : base(targetProperty)
        {
            if (!targetProperty.PropertyType.IsAssignableFrom(by.ParameterType))
            {
                throw new InvalidOperationException($"Property {targetProperty} is not assignable from parameter {by}.");
            }
            By = by ?? throw new ArgumentNullException(nameof(by));
        }

        public override void Accept(IPropertyMappingVisitor visitor)
            => visitor.Visit(this);

        public override T Accept<T>(IPropertyMappingVisitor<T> visitor)
            => visitor.Visit(this);

        public override int CompareTo(PropertyMapping? other)
        {
            if (other is null)
            {
                throw new ArgumentNullException(nameof(other));
            }
            if (other is ByCtorParameterMapping that)
            {
                return _int32Comparer.Compare(By.Position, that.By.Position);
            }
            return -1;
        }

        public override int ComputeHashCode()
            => HashCode.Combine(0, TargetProperty, By);

        public override bool Equals(PropertyMapping? other)
            => other switch
            {
                null => false,
                ByCtorParameterMapping that => TargetProperty.Equals(that.TargetProperty) && By.Equals(that.By),
                _ => false
            };
    }

    public class BySetterMapping : PropertyMapping
    {
        public override bool IsByCtorParameter => false;

        public override bool IsBySetter => true;

        internal BySetterMapping(PropertyInfo targetProperty)
            : base(targetProperty)
        {
            if (!targetProperty.CanWrite)
            {
                throw new InvalidOperationException($"Property {targetProperty} is not writable.");
            }
        }

        public override void Accept(IPropertyMappingVisitor visitor)
            => visitor.Visit(this);

        public override T Accept<T>(IPropertyMappingVisitor<T> visitor)
            => visitor.Visit(this);

        public override int CompareTo(PropertyMapping? other)
        {
            if (other is null)
            {
                throw new ArgumentNullException(nameof(other));
            }
            if (other is BySetterMapping that)
            {
                return _stringComparer.Compare(TargetProperty.Name, that.TargetProperty.Name);
            }
            return 1;
        }

        public override int ComputeHashCode()
            => HashCode.Combine(1, TargetProperty);

        public override bool Equals(PropertyMapping? other)
            => other switch
            {
                null => false,
                BySetterMapping that => TargetProperty.Equals(that.TargetProperty),
                _ => false
            };
    }

    public static ByCtorParameterMapping ByCtorParameter(PropertyInfo targetProperty, ParameterInfo by)
        => new(targetProperty, by);

    public static BySetterMapping BySetter(PropertyInfo targetProperty)
        => new(targetProperty);

    public PropertyInfo TargetProperty { get; }

    public abstract bool IsByCtorParameter { get; }

    public abstract bool IsBySetter { get; }

    internal PropertyMapping(PropertyInfo targetProperty)
        => TargetProperty = targetProperty ?? throw new ArgumentNullException(nameof(targetProperty));

    public abstract void Accept(IPropertyMappingVisitor visitor);

    public abstract T Accept<T>(IPropertyMappingVisitor<T> visitor);

    public T Match<T>(Func<ByCtorParameterMapping, T> onByCtorParameter, Func<BySetterMapping, T> onBySetter)
        => this switch
        {
            ByCtorParameterMapping m => onByCtorParameter(m),
            BySetterMapping m => onBySetter(m),
            _ => throw new InvalidOperationException("Should never happen.")
        };

    public void Match(Action<ByCtorParameterMapping> onByCtorParameter, Action<BySetterMapping> onBySetter)
    {
        switch (this)
        {
            case ByCtorParameterMapping m:
                onByCtorParameter(m);
                break;
            case BySetterMapping m:
                onBySetter(m);
                break;
            default:
                throw new InvalidOperationException("Should never happen.");
        }
    }

    public abstract int CompareTo(PropertyMapping? other);

    public abstract int ComputeHashCode();

    public abstract bool Equals(PropertyMapping? other);

    public override bool Equals(object? obj)
        => obj is PropertyMapping other && Equals(other);

    public override int GetHashCode()
        => ComputeHashCode();
}