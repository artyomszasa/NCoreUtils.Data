using System;
using System.Reflection;

namespace NCoreUtils.Data.Google.FireStore.Builders
{
    public class PropertyDescriptorBuilder
    {
        public TypeDescriptorBuilder Type { get; }

        public ModelBuilder Model => Type.Model;

        public PropertyInfo Property { get; }

        public string Name { get; set; }

        public int ParameterIndex { get; internal set; } = -1;

        public PropertyDescriptorBuilder(TypeDescriptorBuilder type, PropertyInfo property)
        {
            Type = type;
            Property = property ?? throw new ArgumentNullException(nameof(property));
        }

        public PropertyDescriptor Build()
            => new PropertyDescriptor(
                Property,
                Name ?? Model.NamingConvention.Consolidate(Property.Name),
                ParameterIndex);
    }

    public class PropertyDescriptorBuilder<T> : PropertyDescriptorBuilder
    {
        public PropertyDescriptorBuilder(TypeDescriptorBuilder type, PropertyInfo property)
            : base(type, property)
        {
            if (!typeof(T).IsAssignableFrom(property.PropertyType))
            {
                throw new InvalidOperationException($"Property has invalid type: {typeof(T)} !~= {property.PropertyType}.");
            }
        }
    }
}