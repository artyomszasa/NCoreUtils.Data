using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Google.Cloud.Firestore.V1;

namespace NCoreUtils.Data.Google.Cloud.Firestore
{
    public partial class FirestoreConverter
    {
        protected bool TryCollectionToValue(
            object? value,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] Type sourceType,
            [NotNullWhen(true)] out Value? result)
        {
            if (value is null)
            {
                result = default;
                return false;
            }
            // if enumerable?
            Type? elementType = sourceType.IsConstructedGenericType && sourceType.GetGenericTypeDefinition().Equals(typeof(IEnumerable<>))
                ? sourceType.GetGenericArguments()[0]
                : sourceType.GetInterfaces().FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition().Equals(typeof(IEnumerable<>)))?.GetGenericArguments()[0];
            if (elementType is null)
            {
                result = default;
                return false;
            }
            var array = new ArrayValue();
            foreach (var item in (IEnumerable)value)
            {
                array.Values.Add(ConvertToValue(item, elementType));
            }
            result = new Value { ArrayValue = array };
            return true;
        }

        [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Both key and value are preserved types.")]
        [UnconditionalSuppressMessage("Trimming", "IL2062", Justification = "Both key and value are preserved types.")]
        [UnconditionalSuppressMessage("Trimming", "IL2067", Justification = "Both key and value are preserved types.")]
        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(KeyValuePair<,>))]
        protected IEnumerable CollectionFromValue(
            Value value,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type targetType,
            CollectionFactory collectionFactory, bool strictMode)
        {
            if (value.ValueTypeCase == Value.ValueTypeOneofCase.NullValue && !strictMode)
            {
                return collectionFactory.CreateBuilder().Build();
            }
            if (value.ValueTypeCase == Value.ValueTypeOneofCase.ArrayValue)
            {
                var builder = collectionFactory.CreateBuilder();
                var items = value.ArrayValue.Values.Select(selector);
                builder.AddRange(items);
                return builder.Build();

                [UnconditionalSuppressMessage("Trimming", "IL2062", Justification = "Element type should already be preserved.")]
                [UnconditionalSuppressMessage("Trimming", "IL2072", Justification = "Element type should already be preserved.")]
                object? selector(Value item) => ConvertFromValue(item, collectionFactory.ElementType);
            }
            if (value.ValueTypeCase == Value.ValueTypeOneofCase.MapValue && targetType.IsDictionaryType(out var keyType, out var valueType))
            {
                var keyValueType = typeof(KeyValuePair<,>).MakeGenericType(keyType, valueType);
                var builder = collectionFactory.CreateBuilder();
                foreach (var kv in value.MapValue.Fields)
                {
                    var key = keyType == typeof(string) ? kv.Key : Convert.ChangeType(kv.Key, keyType);
                    var val = ConvertFromValue(kv.Value, valueType);
                    builder.Add(Activator.CreateInstance(keyValueType, key, val)!);
                }
                return builder.Build();
            }
            throw new InvalidOperationException($"Unable to convert value of type {value.ValueTypeCase} to {targetType}.");
        }

        protected IEnumerable CollectionFromValue(
            Value value,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type targetType,
            CollectionFactory collectionFactory)
            => CollectionFromValue(value, targetType, collectionFactory, true);
    }
}