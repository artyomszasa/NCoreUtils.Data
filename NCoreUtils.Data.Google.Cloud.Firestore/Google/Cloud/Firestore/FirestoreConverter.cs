using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using Google.Cloud.Firestore.V1;
using Microsoft.Extensions.Logging;

namespace NCoreUtils.Data.Google.Cloud.Firestore;

public partial class FirestoreConverter
{
    internal delegate bool TryGetValueDelegate(string name, [NotNullWhen(true)] out Value? value);

    private sealed class CanConvertPredicate
    {
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
        private Type TargetType { get; }

        public CanConvertPredicate([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type targetType)
        {
            TargetType = targetType;
        }

        public bool Invoke(FirestoreValueConverter c)
            => c.CanConvert(TargetType);
    }

    private readonly ConcurrentDictionary<Type, Ctor> _ctorCache = new();

    private readonly Func<Type, Ctor> _ctorFactory = CtorFactory;

    [UnconditionalSuppressMessage("Trimming", "IL2111")]
    [UnconditionalSuppressMessage("Trimming", "IL2067")]
    private static Ctor CtorFactory(Type type)
        => Ctor.GetCtor(type);

    public ILogger Logger { get; }

    public FirestoreConversionOptions Options { get; }

    public FirestoreModel Model { get; }

    public FirestoreConverter(ILogger<FirestoreConverter> logger, FirestoreConversionOptions options, FirestoreModel model)
    {
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        Options = options ?? throw new ArgumentNullException(nameof(options));
        Model = model ?? throw new ArgumentNullException(nameof(model));
    }

    protected Ctor GetCtor(Type type)
        => _ctorCache.GetOrAdd(type, _ctorFactory);

    public Value ConvertToValue(object? value, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type sourceType)
    {
        // try custom converters
        if (Options.Converters.TryGetFirst(new CanConvertPredicate(sourceType).Invoke, out var customConverter))
        {
            return customConverter.ConvertToValue(value, sourceType, this);
        }
        // try nullable
        if (sourceType.IsNullable(out var elementType))
        {
            if (value is null)
            {
                return new Value { NullValue = default };
            }
            return ConvertToValue(value, elementType);
        }
        // try default
        if (TryPrimitiveToValue(value, sourceType, out var result))
        {
            return result;
        }
        // try as enum
        if (TryEnumToValue(value, sourceType, out result))
        {
            return result;
        }
        // try as collection
        if (TryCollectionToValue(value, sourceType, out result))
        {
            return result;
        }
        // try as entity
        if (Model.TryGetDataEntity(sourceType, out var entity))
        {
            return EntityToValue(value, entity);
        }
        throw new InvalidOperationException($"Unable to convert {value} of type {sourceType} to firestore value.");
    }

    public Value ConvertToValue<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>(T value) => ConvertToValue(value, typeof(T));

    [UnconditionalSuppressMessage("Trimming", "IL2062", Justification = "Element type should already be preserved.")]
    public object? ConvertFromValue(Value value, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type targetType)
    {
        // try custom converters
        if (Options.Converters.TryGetFirst(new CanConvertPredicate(targetType).Invoke, out var customConverter))
        {
            return customConverter.ConvertFromValue(value, targetType, this);
        }
        // try nullable
        if (targetType.IsNullable(out var elementType))
        {
            if (value.ValueTypeCase == Value.ValueTypeOneofCase.NullValue)
            {
                return null;
            }
            return ConvertFromValue(value, elementType);
        }
        // try default
        if (TryPrimitiveFromValue(value, targetType, out var result))
        {
            return result;
        }
        // try as enum
        if (TryEnumFromValue(value, targetType, out result))
        {
            return result;
        }
        // try as collection
        if (CollectionFactory.TryCreate(targetType, out var collectionFactory))
        {
            return CollectionFromValue(value, targetType, collectionFactory, Options.StrictMode);
        }
        // try as entity
        if (Model.TryGetDataEntity(targetType, out var entity))
        {
            return EntityFromValue(value, entity);
        }
        throw new InvalidOperationException($"Unable to convert {value} of type {value.ValueTypeCase} to {targetType}.");
    }

    public T ConvertFromValue<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>(Value value)
        => (T)ConvertFromValue(value, typeof(T))!;
}