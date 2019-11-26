using System;
using System.Collections.Generic;

namespace NCoreUtils.Data.Google.FireStore
{
    // FIMXE: EMIT
    public class DocumentConverter
    {
        static readonly Dictionary<Type, Func<object, object>> _primitiveConverters = new Dictionary<Type, Func<object, object>>
        {
            { typeof(byte), b => (long)(byte)b },
            { typeof(ushort), b => (long)(ushort)b },
            { typeof(uint), b => (long)(uint)b },
            { typeof(sbyte), b => (long)(sbyte)b },
            { typeof(short), b => (long)(short)b },
            { typeof(int), b => (long)(int)b },
            { typeof(long), b => b },
            { typeof(float), b => (double)(float)b },
            { typeof(double), b => (double)b },
            { typeof(string), b => b }
        };

        readonly TypeMapping _typeMapping;

        public DocumentConverter(TypeMapping typeMapping)
        {
            _typeMapping = typeMapping;
        }

        public object PopulateValue(object source)
        {
            if (source is null)
            {
                return null;
            }
            var type = source.GetType();
            if (_primitiveConverters.TryGetValue(type, out var converter))
            {
                return converter(source);
            }
            if (type.IsEnum)
            {
                return source.ToString();
            }
            ref readonly TypeDescriptor typeDescriptor = ref _typeMapping(type);
            var values = new Dictionary<string, object>();
            if (!(type.BaseType is null) && type.BaseType != typeof(object))
            {
                values.Add("$type", typeDescriptor.Name);
            }
            foreach (var property in typeDescriptor.Properties)
            {
                if (!(typeDescriptor.IdProperty.HasValue && typeDescriptor.IdProperty.Value == property))
                {
                    values.Add(property.Name, PopulateValue(property.Property.GetValue(source, null)));
                }
            }
            return values;
        }
    }
}