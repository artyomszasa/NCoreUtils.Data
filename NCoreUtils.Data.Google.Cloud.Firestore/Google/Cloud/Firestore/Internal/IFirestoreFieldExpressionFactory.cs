using System.Linq.Expressions;
using NCoreUtils.Data.Google.Cloud.Firestore.Expressions;
using NCoreUtils.Data.Model;

namespace NCoreUtils.Data.Google.Cloud.Firestore.Internal;

public interface IFirestoreFieldExpressionFactory
{
    FirestoreFieldExpression Create(DataEntity entity, DataProperty property, FirestoreConverter converter, Expression snapshot);
}