using System;
using System.Diagnostics.CodeAnalysis;

namespace NCoreUtils.Data;

public sealed class ReflectionCollectionFactoryFactory : ICollectionFactoryFactory
{
    public bool IsCollection([DynamicallyAccessedMembers((DynamicallyAccessedMemberTypes)(-1))] Type collectionType, [MaybeNullWhen(false)] out Type elementType)
        => CollectionFactory.IsCollection(collectionType, out elementType);

    public bool TryCreate([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type collectionType, [NotNullWhen(true)] out ICollectionFactory? builder)
    {
        if (CollectionFactory.TryCreate(collectionType, out var b))
        {
            builder = b;
            return true;
        }
        builder = default;
        return false;
    }
}