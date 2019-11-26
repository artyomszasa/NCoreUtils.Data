using System;
using FirestoreQuery = Google.Cloud.Firestore.Query;
using DocumentSnapshot = Google.Cloud.Firestore.DocumentSnapshot;

namespace NCoreUtils.Data.Google.FireStore.Queries
{
    public static class ConditionExtensions
    {
        public static FirestoreQuery Apply(this Condition condition, FirestoreQuery query)
            => condition.Operation switch
            {
                Condition.Op.NoOp => query,
                Condition.Op.ArrayContains => query.WhereArrayContains(condition.Path, condition.Value),
                Condition.Op.EqualTo => query.WhereEqualTo(condition.Path, condition.Value),
                Condition.Op.GreaterThan => query.WhereGreaterThan(condition.Path, condition.Value),
                Condition.Op.GreaterThanOrEqualTo => query.WhereGreaterThanOrEqualTo(condition.Path, condition.Value),
                Condition.Op.LessThan => query.WhereLessThan(condition.Path, condition.Value),
                Condition.Op.LessThanOrEqualTo => query.WhereLessThanOrEqualTo(condition.Path, condition.Value),
                _ => throw new InvalidOperationException($"Invalid condition {condition}."),
            };

        // FIXME
        public static bool Eval(this Condition condition, DocumentSnapshot snapshot)
            => throw new NotImplementedException("FIXME");
    }
}