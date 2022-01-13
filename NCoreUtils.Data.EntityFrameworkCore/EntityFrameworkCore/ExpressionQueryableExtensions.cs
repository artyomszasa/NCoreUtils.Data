using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Query;

namespace NCoreUtils.Data.EntityFrameworkCore;

public static class ExpressionQueryableExtensions
{
    /// <summary>
    /// Attempts to extract query object from the expression.
    /// </summary>
    /// <param name="source">Source expression.</param>
    /// <param name="provider">Variable to store query provider object.</param>
    /// <param name="extensionTypes">Optional extensions types to accept.</param>
    /// <returns>
    /// <c>true</c> if query provider object has been successfully extracted from the expression and stored into
    /// <paramref name="provider" />, <c>false</c> otherwise.
    /// </returns>
    public static bool TryExtractQueryProvider(
        this Expression source,
        [MaybeNullWhen(false)] out IAsyncQueryProvider provider,
        params Type[] extensionTypes)
    {
        if (source is null)
        {
            throw new ArgumentNullException(nameof(source));
        }
        if (source is QueryRootExpression queryRoot)
        {
            if (queryRoot.QueryProvider is null)
            {
                throw new InvalidOperationException("Query provider of query root expression is null.");
            }
            provider = queryRoot.QueryProvider;
            return true;
        }
        if (source is MethodCallExpression call)
        {
            if (call.Method.IsStatic && (call.Method.DeclaringType == typeof(Queryable) || Array.IndexOf(extensionTypes, call.Method.DeclaringType) >= 0) && call.Arguments.Count > 0)
            {
                var arg0 = call.Arguments[0];
                return arg0.TryExtractQueryProvider(out provider, extensionTypes);
            }
        }
        provider = default;
        return false;
    }
}