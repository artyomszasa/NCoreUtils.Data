using System;
using System.Collections.Generic;

namespace NCoreUtils.Data
{
    public static class FirestoreQueryableExtensions
    {
        public static bool ContainsAny<T>(this IEnumerable<T> source, IEnumerable<T> values)
            => throw new InvalidOperationException($"{nameof(ContainsAny)}(...) can only be used at firestore predicates.");

        public static TProp ShadowProperty<TEntity, TProp>(TEntity entity, string name)
            => throw new InvalidOperationException($"{nameof(ShadowProperty)}(...) can only be used at firestore predicates.");
    }
}