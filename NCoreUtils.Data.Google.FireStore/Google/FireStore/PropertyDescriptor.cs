using System;
using System.Reflection;

namespace NCoreUtils.Data.Google.FireStore
{
    public struct PropertyDescriptor : IEquatable<PropertyDescriptor>
    {
        public static bool operator==(PropertyDescriptor a, PropertyDescriptor b) => a.Equals(b);

        public static bool operator!=(PropertyDescriptor a, PropertyDescriptor b) => !a.Equals(b);

        public PropertyInfo Property { get; }

        public string Name { get; }

        public int ParameterIndex { get; }

        public PropertyDescriptor(PropertyInfo property, string name, int parameterIndex = -1)
        {
            Property = property;
            Name = name;
            ParameterIndex = parameterIndex;
        }

        public override bool Equals(object obj) => obj is PropertyDescriptor other && Equals(other);

        public bool Equals(PropertyDescriptor other)
            => StringComparer.InvariantCultureIgnoreCase.Equals(Name, other.Name)
                && Property.IsSameOrOverridden(other.Property);

        public override int GetHashCode()
            => HashCode.Combine(
                Property,
                Name is null ? 0 : StringComparer.InvariantCultureIgnoreCase.GetHashCode(Name)
            );
    }
}