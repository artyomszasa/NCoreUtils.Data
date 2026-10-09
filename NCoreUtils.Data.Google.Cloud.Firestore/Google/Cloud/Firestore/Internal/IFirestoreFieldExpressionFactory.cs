using System.Linq.Expressions;
using NCoreUtils.Data.Google.Cloud.Firestore.Expressions;
using NCoreUtils.Data.Model;

namespace NCoreUtils.Data.Google.Cloud.Firestore.Internal;

public interface IFirestoreFieldExpressionFactory
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1716:Identifiers should not match keywords", Justification = "Name is suitable here.")]
    FirestoreFieldExpression Create(DataEntity entity, DataProperty property, FirestoreConverter converter, Expression snapshot);
}