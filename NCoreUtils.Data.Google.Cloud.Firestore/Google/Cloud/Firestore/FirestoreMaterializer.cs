using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Google.Cloud.Firestore;
using NCoreUtils.Data.Google.Cloud.Firestore.Expressions;

namespace NCoreUtils.Data.Google.Cloud.Firestore;

public class FirestoreMaterializer
{
    private readonly ConcurrentDictionary<Ctor, object> _ctorExpressionCache = new();

    // FIXME: Expression parameterization and cache.
    protected virtual Func<DocumentSnapshot, T> CompileMaterialization<T>(Expression<Func<DocumentSnapshot, T>> expression)
    {
        if (expression.Body is CtorExpression ctorExpression && ctorExpression.Arguments.All(e => e is FirestoreFieldExpression ex && ex.Instance.Equals(expression.Parameters[0])))
        {
            if (_ctorExpressionCache.TryGetValue(ctorExpression.Ctor, out var boxed))
            {
                return (Func<DocumentSnapshot, T>)boxed;
            }
            return (Func<DocumentSnapshot, T>)_ctorExpressionCache.GetOrAdd(
                ctorExpression.Ctor,
                _ => expression.Compile()
            );
        }
        return expression.Compile();
    }

    public T Materialize<T>(DocumentSnapshot document, Expression<Func<DocumentSnapshot, T>> selector)
        => CompileMaterialization(selector)(document);

    public IEnumerable<T> Materialize<T>(IEnumerable<DocumentSnapshot> documents, Expression<Func<DocumentSnapshot, T>> selector)
        => documents.Select(CompileMaterialization(selector));

    public IAsyncEnumerable<T> Materialize<T>(IAsyncEnumerable<DocumentSnapshot> documents, Expression<Func<DocumentSnapshot, T>> selector)
        => documents.Select(CompileMaterialization(selector));
}