using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using NCoreUtils.Data.Mapping;

namespace NCoreUtils.Data
{
    public abstract class CollectionFactory
    {
        #if NETSTANDARD2_1
        private static bool IsCollection(Type collectionType, [NotNullWhen(true)] out Type? elementType)
        #else
        private static bool IsCollection(Type collectionType, out Type elementType)
        #endif
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
            if (collectionType.GetInterfaces().TryGetFirst(ty => ty.IsGenericType && ty.GetGenericTypeDefinition() == typeof(ICollection<>), out var ifaceType))
            {
                elementType = ifaceType.GetGenericArguments()[0];
                return true;
            }
            #if NETSTANDARD2_1
            elementType = default;
            #else
            elementType = default!;
            #endif
            return false;
        }

        #if NETSTANDARD2_1
        public static bool TryCreate(Type collectionType, [NotNullWhen(true)] out CollectionFactory? builder)
        #else
        public static bool TryCreate(Type collectionType, out CollectionFactory builder)
        #endif
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
            #if NETSTANDARD2_1
            builder = default;
            #else
            builder = default!;
            #endif
            return false;
        }

        public Type ElementType { get; }

        public Type CollectionType { get; }

        internal CollectionFactory(Type elementType, Type collectionType)
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