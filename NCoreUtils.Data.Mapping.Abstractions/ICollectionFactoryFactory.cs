using System;
using System.Diagnostics.CodeAnalysis;

namespace NCoreUtils.Data;

public interface ICollectionFactoryFactory
{
    bool IsCollection(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type collectionType,
        [MaybeNullWhen(false)] out Type elementType)
    {
        if (collectionType.IsInterface)
        {
            elementType = default;
            return false;
        }
        foreach (var ty in collectionType.GetInterfaces())
        {
            if (ty.IsGenericType && ty.GetGenericTypeDefinition() == typeof(System.Collections.Generic.ICollection<>))
            {
                elementType = ty.GetGenericArguments()[0];
                return true;
            }
        }
        elementType = default;
        return false;
    }

    bool TryCreate(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type collectionType,
        [MaybeNullWhen(false)] out ICollectionFactory builder
    );
}