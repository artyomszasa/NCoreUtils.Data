using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace NCoreUtils.Data.Google.Cloud.Firestore.Internal
{
    public abstract class CollectionWrapper
    {
        private static bool IsReadOnlyList(Type collectionType, [NotNullWhen(true)] out Type? elementType)
        {
            if (collectionType.IsInterface)
            {
                #if NETSTANDARD2_1
                elementType = default;
                #else
                elementType = default!;
                #endif
                return false;
            }
            if (collectionType.GetInterfaces().TryGetFirst(ty => ty.IsGenericType && ty.GetGenericTypeDefinition() == typeof(IReadOnlyList<>), out var ifaceType))
            {
                elementType = ifaceType.GetGenericArguments()[0];
                return true;
            }
            elementType = default;
            return false;
        }

        private static bool IsEnumerable(Type collectionType, [NotNullWhen(true)] out Type? elementType)
        {
            if (collectionType.IsInterface)
            {
                #if NETSTANDARD2_1
                elementType = default;
                #else
                elementType = default!;
                #endif
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
                    typeof(ArrayWrapper<>).MakeGenericType(type.GetElementType()),
                    new object[] { source }
                );
            }
            if (IsReadOnlyList(type, out var elementType))
            {
                return (CollectionWrapper)Activator.CreateInstance(
                    typeof(ReadOnlyListWrapper<>).MakeGenericType(elementType),
                    new object[] { source }
                );
            }
            if (IsEnumerable(type, out elementType))
            {
                return (CollectionWrapper)Activator.CreateInstance(
                    typeof(EnumerableWrapper<>).MakeGenericType(elementType),
                    new object[] { source }
                );
            }
            throw new InvalidOperationException($"Unable to create collection wrapper for \"{type}\".");
        }

        public abstract int Count { get; }

        public abstract void SplitIntoChunks(int chunkSize, List<object> results);
    }
}