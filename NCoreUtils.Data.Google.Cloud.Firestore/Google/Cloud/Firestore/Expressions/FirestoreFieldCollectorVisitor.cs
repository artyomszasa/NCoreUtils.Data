using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using Google.Cloud.Firestore;

namespace NCoreUtils.Data.Google.Cloud.Firestore.Expressions;

public class FirestoreFieldCollectorVisitor : ExpressionVisitor
{
    private static readonly MethodInfo _gmShadowProperty
        = GetMethod<object, string, string>(FirestoreQueryableExtensions.ShadowProperty<object, string>).GetGenericMethodDefinition();

    private static MethodInfo GetMethod<TArg1, TArg2, TResult>(Func<TArg1, TArg2, TResult> func) => func.Method;

    private readonly HashSet<FieldPath> _paths = [];

    public IReadOnlyCollection<FieldPath> Paths => _paths;

    public override Expression? Visit(Expression? node)
    {
        if (node is FirestoreFieldExpression fieldExpression)
        {
            _paths.Add(fieldExpression.Path);
            return node;
        }
        if (node is MethodCallExpression m && m.Method.IsConstructedGenericMethod
            && m.Method.GetGenericMethodDefinition() == _gmShadowProperty
            && m.Arguments[1].TryExtractConstant(out var boxedShadowPath) && boxedShadowPath is string shadowPath)
        {
            // FIXME: handle subpathes
            _paths.Add(new FieldPath(shadowPath));
            return node;
        }
        return base.Visit(node);
    }
}