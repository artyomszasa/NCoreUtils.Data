using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using NCoreUtils.Data.Google.Cloud.Firestore.Internal;

namespace NCoreUtils.Data.Build;

public static class FirestoreReflectionDecorator
{
    private class ReflectionCollectionFactoryFactory : ICollectionFactoryFactory
    {
        public bool TryCreate(
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type collectionType,
            [MaybeNullWhen(false)] out ICollectionFactory builder)
        {
            if (CollectionFactory.TryCreate(collectionType, out var factory))
            {
                builder = factory;
                return true;
            }
            builder = default;
            return false;
        }
    }

    private class ReflectionCollectionWrapperFactory : ICollectionWrapperFactory
    {
        private abstract class CollectionWrapperInitializer
        {
            public abstract ICollectionWrapper Initialize(object source);
        }

        private sealed class CollectionWrapperInitializer<T> : CollectionWrapperInitializer
        {
            public override ICollectionWrapper Initialize(object source)
                => new AnyCollectionWrapper<T>((IEnumerable<T>)source);
        }

        private static ConcurrentDictionary<Type, CollectionWrapperInitializer> Cache { get; } = new();

        public bool TryCreate(object source, [MaybeNullWhen(false)] out ICollectionWrapper wrapper)
        {
            if (source is null)
            {
                wrapper = default;
                return false;
            }
            var type = source.GetType();
            if (IsEnumerable(type, out var elementType))
            {
                if (!Cache.TryGetValue(type, out var initializer))
                {
                    initializer = (CollectionWrapperInitializer)Activator.CreateInstance(typeof(CollectionWrapperInitializer<>).MakeGenericType(elementType), true)!;
                    Cache.TryAdd(type, initializer);
                }
                wrapper = initializer.Initialize(source);
                return true;
            }
            wrapper = default;
            return false;
        }
    }

    private static bool IsEnumerable(Type collectionType, [NotNullWhen(true)] out Type? elementType)
    {
        if (collectionType.IsInterface)
        {
            elementType = default;
            return false;
        }
        if (collectionType.GetInterfaces().TryGetFirst(ty => ty.IsGenericType && ty.GetGenericTypeDefinition() == typeof(IEnumerable<>), out var ifaceType))
        {
            elementType = ifaceType.GetGenericArguments()[0];
            return true;
        }
        elementType = default;
        return false;
    }

    public static DataModelBuilder AddReflectionBasedFirestoreDecorations(this DataModelBuilder model)
    {
        var enumTypes = new HashSet<Type>();
        var fieldExpressionFactory = new ReflectionFieldExpressionFactory();
        foreach (var entityBuilder in model.Entities)
        {
            foreach (var (property, propertyBuilder) in entityBuilder.Properties)
            {
                if (property.PropertyType.IsEnum)
                {
                    enumTypes.Add(property.PropertyType);
                }
                propertyBuilder.SetMetadata(FirestoreMetadataExtensions.KeyFieldExpressionFactory, fieldExpressionFactory);
            }
        }
        model.SetMetadata(FirestoreMetadataExtensions.KeyEnumConversionHelpers, new ReflectionEnumConversionHelpers(enumTypes));
        model.SetMetadata(FirestoreMetadataExtensions.KeyCollectionFactoryFactory, new ReflectionCollectionFactoryFactory());
        model.SetMetadata(FirestoreMetadataExtensions.KeyCollectionWrapperFactory, new ReflectionCollectionWrapperFactory());
        return model;
    }
}