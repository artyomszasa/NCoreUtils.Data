using System;
using System.Collections.Generic;
using System.Reflection;

namespace NCoreUtils.Data
{
    public abstract class PropertyMapping : IComparable<PropertyMapping>
    {
        private static Comparer<int> _int32Comparer = Comparer<int>.Default;

        private static StringComparer _stringComparer = StringComparer.InvariantCulture;

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

            public override int CompareTo(PropertyMapping other)
            {
                if (other is ByCtorParameterMapping that)
                {
                    return _int32Comparer.Compare(By.Position, that.By.Position);
                }
                return -1;
            }
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

            public override int CompareTo(PropertyMapping other)
            {
                if (other is BySetterMapping that)
                {
                    return _stringComparer.Compare(TargetProperty.Name, that.TargetProperty.Name);
                }
                return 1;
            }
        }

        public static ByCtorParameterMapping ByCtorParameter(PropertyInfo targetProperty, ParameterInfo by)
            => new ByCtorParameterMapping(targetProperty, by);

        public static BySetterMapping BySetter(PropertyInfo targetProperty)
            => new BySetterMapping(targetProperty);

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

        public abstract int CompareTo(PropertyMapping other);
    }
}