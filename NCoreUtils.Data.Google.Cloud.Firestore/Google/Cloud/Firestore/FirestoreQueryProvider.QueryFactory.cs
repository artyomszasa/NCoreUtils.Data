using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Linq.Expressions;
using Google.Cloud.Firestore;
using NCoreUtils.Data.Google.Cloud.Firestore.Expressions;

namespace NCoreUtils.Data.Google.Cloud.Firestore
{
    public partial class FirestoreQueryProvider
    {
        public FirestoreQuery<T> CreateQueryable<T>()
        {
            if (!Model.TryGetDataEntity(typeof(T), out var entity))
            {
                throw new InvalidOperationException($"Unable to create initial selector for type {typeof(T)} as it is not registered as entity.");
            }
            return new FirestoreQuery<T>(
                this,
                entity.Name,
                Model.GetInitialSelector<T>(),
                ImmutableList<FirestoreCondition>.Empty,
                ImmutableList<FirestoreOrdering>.Empty,
                0,
                default
            );
        }
    }
}