using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using NCoreUtils.Data.Mapping;

namespace NCoreUtils.Data
{
    public abstract class CollectionFactory
    {
        public static bool IsCollection(
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type collectionType,
            [NotNullWhen(true)] out Type? elementType)
        {
            if (collectionType.IsInterface)
            {
                elementType = default;
                return false;
            }
            if (collectionType.GetInterfaces().TryGetFirst(ty => ty.IsGenericType && ty.GetGenericTypeDefinition() == typeof(ICollection<>), out var ifaceType))
            {
                elementType = ifaceType.GetGenericArguments()[0];
                return true;
            }
            elementType = default;
            return false;
        }

        public static bool TryCreate(
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type collectionType,
            [NotNullWhen(true)] out CollectionFactory? builder)
        {
            if (IsCollection(collectionType, out var elementType))
            {
                builder = new MutableCollectionFactory(elementType, collectionType);
                return true;
            }
            if (collectionType.IsGenericType && collectionType.GetGenericTypeDefinition() == typeof(IReadOnlyList<>))
            {
                elementType = collectionType.GetGenericArguments()[0];
                builder = new ReadOnlyListFactory(elementType, collectionType);
                return true;
            }
            builder = default;
            return false;
        }

        public Type ElementType { get; }

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
        public Type CollectionType { get; }

        internal CollectionFactory(
            Type elementType,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type collectionType)
        {
            ElementType = elementType ?? throw new ArgumentNullException(nameof(elementType));
            CollectionType = collectionType ?? throw new ArgumentNullException(nameof(collectionType));
        }

        public abstract Expression CreateNewExpression(IEnumerable<Expression> items);

        /// <summary>
        /// Creates construction expression from single parameter that must be enumerable sequence of items.
        /// </summary>
        /// <param name="items">Expression representing enumerable source.</param>
        public abstract Expression CreateNewExpression(Expression items);

        public abstract ICollectionBuilder CreateBuilder();
    }
}