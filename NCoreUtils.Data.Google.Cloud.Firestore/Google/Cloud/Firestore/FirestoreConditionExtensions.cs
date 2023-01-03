using System;
using System.Collections.Generic;
using Google.Cloud.Firestore;

namespace NCoreUtils.Data.Google.Cloud.Firestore
{
    public static class FireStoreConditionExtensions
    {
        private static object AdaptValue(Query query, IFirestoreConfiguration configuration, string collection, FieldPath path, object value)
        {
            if (FieldPath.DocumentId.Equals(path) && value is string svalue)
            {
                return query.Database.Collection(collection).Document(svalue);
            }
            if (value is not null)
            {
                var copts = configuration.ConversionOptions;
                if (copts is not null && copts.Converters.TryGetFirst(c => c.CanConvert(value.GetType()), out var converter))
                {
                    return converter.ConvertToConditionArgument(value)!;
                }
            }
            return value!;
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

        public static Query Apply(this FirestoreCondition condition, Query query, IFirestoreConfiguration configuration, string collection)
            => condition.Operation switch
            {
                FirestoreCondition.Op.NoOp => query,
                FirestoreCondition.Op.ArrayContains => query.WhereArrayContains(condition.Path, AdaptValues(query, collection, condition.Path, condition.Value!)),
                FirestoreCondition.Op.In => query.WhereIn(condition.Path, (System.Collections.IEnumerable)AdaptValues(query, collection, condition.Path, condition.Value!)),
                FirestoreCondition.Op.EqualTo => query.WhereEqualTo(condition.Path, AdaptValue(query, configuration, collection, condition.Path, condition.Value!)),
                FirestoreCondition.Op.GreaterThan => query.WhereGreaterThan(condition.Path, AdaptValue(query, configuration, collection, condition.Path, condition.Value!)),
                FirestoreCondition.Op.GreaterThanOrEqualTo => query.WhereGreaterThanOrEqualTo(condition.Path, AdaptValue(query, configuration, collection, condition.Path, condition.Value!)),
                FirestoreCondition.Op.LessThan => query.WhereLessThan(condition.Path, AdaptValue(query, configuration, collection, condition.Path, condition.Value!)),
                FirestoreCondition.Op.LessThanOrEqualTo => query.WhereLessThanOrEqualTo(condition.Path, AdaptValue(query, configuration, collection, condition.Path, condition.Value!)),
                FirestoreCondition.Op.ArrayContainsAny => query.WhereArrayContainsAny(condition.Path, (System.Collections.IEnumerable)condition.Value!),
                FirestoreCondition.Op.AlwaysFalse => throw new InvalidOperationException($"Always false condition not supposed to be applied"),
                _ => throw new InvalidOperationException($"Invalid condition {condition}."),
            };
    }
}