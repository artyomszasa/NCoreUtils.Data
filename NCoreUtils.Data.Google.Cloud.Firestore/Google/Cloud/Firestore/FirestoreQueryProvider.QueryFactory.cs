using System;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace NCoreUtils.Data.Google.Cloud.Firestore
{
    public partial class FirestoreQueryProvider
    {
        public FirestoreQuery<T> CreateQueryable<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)] T>()
        {
            if (!Model.TryGetDataEntity(typeof(T), out var entity))
            {
                throw new InvalidOperationException($"Unable to create initial selector for type {typeof(T)} as it is not registered as entity.");
            }
            return new FirestoreQuery<T>(
                this,
                entity.Name,
                Model.GetInitialSelector<T>(),
                ImmutableHashSet<FirestoreCondition>.Empty,
                ImmutableList<FirestoreOrdering>.Empty,
                0,
                default
            );
        }
    }
}