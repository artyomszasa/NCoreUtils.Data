using System.Linq;

namespace NCoreUtils.Data.Google.Cloud.Firestore;

public static class FirestoreQueryExtensions
{
    public static bool IsAlwaysFalse(this FirestoreQuery query)
        => query.Conditions.Any(c => c.Operation == FirestoreCondition.Op.AlwaysFalse);
}