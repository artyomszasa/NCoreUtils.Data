using System;
using Google.Cloud.Firestore;

namespace NCoreUtils.Data.Google.Cloud.Firestore
{
    public static class FireStoreConditionExtensions
    {
        public static Query Apply(this FirestoreCondition condition, Query query)
            => condition.Operation switch
            {
                FirestoreCondition.Op.NoOp => query,
                FirestoreCondition.Op.ArrayContains => query.WhereArrayContains(condition.Path, condition.Value),
                FirestoreCondition.Op.EqualTo => query.WhereEqualTo(condition.Path, condition.Value),
                FirestoreCondition.Op.GreaterThan => query.WhereGreaterThan(condition.Path, condition.Value),
                FirestoreCondition.Op.GreaterThanOrEqualTo => query.WhereGreaterThanOrEqualTo(condition.Path, condition.Value),
                FirestoreCondition.Op.LessThan => query.WhereLessThan(condition.Path, condition.Value),
                FirestoreCondition.Op.LessThanOrEqualTo => query.WhereLessThanOrEqualTo(condition.Path, condition.Value),
                _ => throw new InvalidOperationException($"Invalid condition {condition}."),
            };
    }
}