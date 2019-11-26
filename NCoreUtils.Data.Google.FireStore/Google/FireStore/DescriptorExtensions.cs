using System.Collections.Generic;
using System.Reflection;

namespace NCoreUtils.Data.Google.FireStore
{
    public static class DescriptorExtensions
    {
        public static bool TryGetPropertyDescriptor(this in TypeDescriptor typeDescriptor, PropertyInfo property, out PropertyDescriptor propertyDescriptor)
            => typeDescriptor.PropertyMap.TryGetValue(property, out propertyDescriptor);

        public static bool TryGetPropertyName(this in TypeDescriptor typeDescriptor, PropertyInfo property, out string name)
        {
            if (typeDescriptor.TryGetPropertyDescriptor(property, out var propertyDescriptor))
            {
                name = propertyDescriptor.Name;
                return true;
            }
            name = default;
            return false;
        }

        public static PropertyDescriptor GetPropertyDescriptor(this in TypeDescriptor typeDescriptor, PropertyInfo property)
        {
            if (typeDescriptor.TryGetPropertyDescriptor(property, out var propertyDescriptor))
            {
                return propertyDescriptor;
            }
            throw new KeyNotFoundException($"No mapping found for {property.DeclaringType.FullName}.{property.Name}.");
        }

        public static string GetPropertyName(this in TypeDescriptor typeDescriptor, PropertyInfo property)
            => typeDescriptor.GetPropertyDescriptor(property).Name;
    }
}