using System;
using System.Collections.Generic;
using Google.Cloud.Firestore;

namespace NCoreUtils.Data.Google.Cloud.Firestore
{
    public static class FireStoreConditionExtensions
    {
        private static object AdaptValue(Query query, string collection, FieldPath path, object value)
        {
            if (FieldPath.DocumentId.Equals(path) && value is string svalue)
            {
                return query.Database.Collection(collection).Document(svalue);
            }
            return value;
        }

        private static object AdaptValues(Query query, string collection, FieldPath path, object value)
        {
            if (FieldPath.DocumentId.Equals(path) && value is IEnumerable<string> sarray)
            {
                var col = query.Database.Collection(collection);
                return sarray.MapToArray(id => col.Document(id));
            }
            return value;
        }

        public static Query Apply(this FirestoreCondition condition, Query query, string collection)
            => condition.Operation switch
            {
                FirestoreCondition.Op.NoOp => query,
                FirestoreCondition.Op.ArrayContains => query.WhereArrayContains(condition.Path, AdaptValues(query, collection, condition.Path, condition.Value)),
                FirestoreCondition.Op.EqualTo => query.WhereEqualTo(condition.Path, AdaptValue(query, collection, condition.Path, condition.Value)),
                FirestoreCondition.Op.GreaterThan => query.WhereGreaterThan(condition.Path, AdaptValue(query, collection, condition.Path, condition.Value)),
                FirestoreCondition.Op.GreaterThanOrEqualTo => query.WhereGreaterThanOrEqualTo(condition.Path, AdaptValue(query, collection, condition.Path, condition.Value)),
                FirestoreCondition.Op.LessThan => query.WhereLessThan(condition.Path, AdaptValue(query, collection, condition.Path, condition.Value)),
                FirestoreCondition.Op.LessThanOrEqualTo => query.WhereLessThanOrEqualTo(condition.Path, AdaptValue(query, collection, condition.Path, condition.Value)),
                FirestoreCondition.Op.ArrayContainsAny => query.WhereArrayContainsAny(condition.Path, (System.Collections.IEnumerable)condition.Value),
                _ => throw new InvalidOperationException($"Invalid condition {condition}."),
            };
    }
}