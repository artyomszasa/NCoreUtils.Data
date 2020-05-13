using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq.Expressions;
using Google.Cloud.Firestore;

namespace NCoreUtils.Data.Google.Cloud.Firestore.Expressions
{
    public class FirestoreCollectionFieldExpression : FirestoreFieldExpression
    {
        public CollectionBuilder CollectionBuilder { get; }

        public FirestoreCollectionFieldExpression(
            Expression instance,
            ImmutableList<string> rawPath,
            Type type,
            CollectionBuilder collectionBuilder)
            : base(instance, rawPath, type)
        {
            CollectionBuilder = collectionBuilder ?? throw new ArgumentNullException(nameof(collectionBuilder));
        }

        public FirestoreCollectionFieldExpression(
            Expression instance,
            FieldPath specialPath,
            Type type,
            CollectionBuilder collectionBuilder)
            : base(instance, specialPath, type)
        {
            CollectionBuilder = collectionBuilder ?? throw new ArgumentNullException(nameof(collectionBuilder));
        }

        public override Expression Reduce()
        {
            var enumerable = Reduce(typeof(List<>).MakeGenericType(CollectionBuilder.ElementType));
            var result = CollectionBuilder.CreateNewExpression(enumerable);
            return result;
        }
    }
}