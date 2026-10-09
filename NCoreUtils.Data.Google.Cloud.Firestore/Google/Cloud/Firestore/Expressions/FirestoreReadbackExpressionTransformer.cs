using System.Linq.Expressions;

namespace NCoreUtils.Data.Google.Cloud.Firestore.Expressions;

public sealed class FirestoreReadbackExpressionTransformer(Expression valueDictionary, Expression id)
    : ExpressionVisitor
{
    public Expression ValueDictionary { get; } = Preconditions.ThrowIfNull(valueDictionary);

    protected override Expression VisitExtension(Expression node)
    {
        if (node is FirestoreFieldExpression expr)
        {
            return Visit(expr.ToReadBackExpression(valueDictionary, id));
        }
        return base.VisitExtension(node);
    }
}