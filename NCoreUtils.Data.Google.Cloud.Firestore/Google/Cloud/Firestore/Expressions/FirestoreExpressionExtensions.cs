using System.Collections.Generic;
using System.Linq.Expressions;
using Google.Cloud.Firestore;

namespace NCoreUtils.Data.Google.Cloud.Firestore.Expressions;

public static class FirestoreExpressionExtensions
{
    public static IReadOnlyCollection<FieldPath> CollectFirestorePaths(this Expression expression)
    {
        var visitor = new FirestoreFieldCollectorVisitor();
        visitor.Visit(expression);
        return visitor.Paths;
    }
}