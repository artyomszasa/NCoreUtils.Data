using System;
using System.Linq;
using Google.Cloud.Firestore.V1;
using NCoreUtils.Data.Model;

namespace NCoreUtils.Data.Google.Cloud.Firestore
{
    public partial class FirestoreConverter
    {
        protected Value EntityToValue(object? value, DataEntity entity)
        {
            var map = new MapValue();
            foreach (var property in entity.Properties)
            {
                var propertyValue = property.Property.GetValue(value, null);
                map.Fields.Add(property.Name, ConvertToValue(propertyValue, property.Property.PropertyType));
            }
            return new Value { MapValue = map };
        }

        internal object? EntityFromValue(TryGetValueDelegate tryGetValue, DataEntity entity)
        {
            var ctor = GetCtor(entity.EntityType);
            return ctor.Instantiate(ctor.Properties
                .Select(p => entity.Properties.First(dp => dp.Property == p.TargetProperty))
                .Select(dp =>
                {
                    if (tryGetValue(dp.Name, out var subvalue))
                    {
                        return ConvertFromValue(subvalue, dp.Property.PropertyType);
                    }
                    if (dp.TryGetDefaultValue(out var defaultValue))
                    {
                        return defaultValue;
                    }
                    if (true != dp.Required)
                    {
                        return dp.Property.PropertyType.IsValueType ? Activator.CreateInstance(dp.Property.PropertyType) : default;
                    }
                    throw new InvalidOperationException($"No value found for required property without default value {dp}.");
                })
                .ToArray()
            );
        }

        protected object? EntityFromValue(Value value, DataEntity entity)
        {
            if (value.ValueTypeCase == Value.ValueTypeOneofCase.NullValue)
            {
                return default;
            }
            if (value.ValueTypeCase != Value.ValueTypeOneofCase.MapValue)
            {
                throw new InvalidOperationException($"Unable to convert {value} of type {value.ValueTypeCase} to {entity.EntityType}.");
            }
            var fields = value.MapValue.Fields;
            return EntityFromValue((string name, out Value? value) => fields.TryGetValue(name, out value), entity);
        }
    }
}