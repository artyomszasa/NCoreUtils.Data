using System.Collections.Generic;
using System.Linq.Expressions;
using Google.Cloud.Firestore;

namespace NCoreUtils.Data.Google.Cloud.Firestore.Expressions
{
    public class FirestoreFieldCollectorVisitor : ExpressionVisitor
    {
        private readonly HashSet<FieldPath> _paths = new();

        public IReadOnlyCollection<FieldPath> Paths => _paths;

        public override Expression? Visit(Expression? node)
        {
            if (node is FirestoreFieldExpression fieldExpression)
            {
                _paths.Add(fieldExpression.Path);
                return node;
            }
            return base.Visit(node);
        }
    }
}