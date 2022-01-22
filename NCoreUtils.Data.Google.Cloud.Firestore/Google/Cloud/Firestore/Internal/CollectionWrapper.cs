using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace NCoreUtils.Data.Google.Cloud.Firestore.Internal
{
    public abstract class CollectionWrapper
    {
        private static bool IsReadOnlyList(
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] Type collectionType,
            [NotNullWhen(true)] out Type? elementType)
        {
            if (collectionType.IsInterface)
            {
                elementType = default;
                return false;
            }
            if (collectionType.GetInterfaces().TryGetFirst(ty => ty.IsConstructedGenericType && ty.GetGenericTypeDefinition() == typeof(IReadOnlyList<>), out var ifaceType))
            {
                elementType = ifaceType.GetGenericArguments()[0];
                return true;
            }
            elementType = default;
            return false;
        }

        private static bool IsEnumerable(
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] Type collectionType,
            [NotNullWhen(true)] out Type? elementType)
        {
            if (collectionType.IsInterface)
            {
                elementType = default;
                return false;
            }
            if (collectionType.GetInterfaces().TryGetFirst(ty => ty.IsConstructedGenericType && ty.GetGenericTypeDefinition() == typeof(IEnumerable<>), out var ifaceType))
            {
                elementType = ifaceType.GetGenericArguments()[0];
                return true;
            }
            elementType = default;
            return false;
        }

        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(ArrayWrapper<>))]
        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(ReadOnlyListWrapper<>))]
        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(EnumerableWrapper<>))]
        [UnconditionalSuppressMessage("Trimming", "IL2026")]
        [UnconditionalSuppressMessage("Trimming", "IL2072")]
        [UnconditionalSuppressMessage("Trimming", "IL2111")]
        public static CollectionWrapper Create(object source)
        {
            if (source is null)
            {
                throw new ArgumentNullException(nameof(source));
            }
            var type = source.GetType();
            if (type.IsArray)
            {
                return (CollectionWrapper)Activator.CreateInstance(
                    typeof(ArrayWrapper<>).MakeGenericType(type.GetElementType()!),
                    new object[] { source }
                )!;
            }
            if (IsReadOnlyList(type, out var elementType))
            {
                return (CollectionWrapper)Activator.CreateInstance(
                    typeof(ReadOnlyListWrapper<>).MakeGenericType(elementType),
                    new object[] { source }
                )!;
            }
            if (IsEnumerable(type, out elementType))
            {
                return (CollectionWrapper)Activator.CreateInstance(
                    typeof(EnumerableWrapper<>).MakeGenericType(elementType),
                    new object[] { source }
                )!;
            }
            throw new InvalidOperationException($"Unable to create collection wrapper for \"{type}\".");
        }

        public abstract int Count { get; }

        public abstract void SplitIntoChunks(int chunkSize, List<object> results);
    }
}