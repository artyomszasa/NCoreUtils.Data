using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Google.Cloud.Firestore;
using NCoreUtils.Data.Google.Cloud.Firestore.Expressions;

namespace NCoreUtils.Data.Google.Cloud.Firestore
{
    public partial class FirestoreQueryProvider
    {
        private sealed class EmptyAsyncEnumerator<T> : IAsyncEnumerator<T>
        {
            public T Current => default!;

            public ValueTask DisposeAsync() => default;

            public ValueTask<bool> MoveNextAsync() => default;
        }

        private sealed class EmptyAsyncEnumerable<T> : IAsyncEnumerable<T>
        {
            private static readonly EmptyAsyncEnumerator<T> _enumerator = new();

            public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
                => _enumerator;
        }

        [Obsolete("TryResolveSubpath(...) is an internal mathod, use TryResolvePath(...).")]
        private bool TryResolveSubpath(
            Expression expression,
            ParameterExpression document,
            ImmutableList<string> propertyPath,
            [NotNullWhen(true)] out FieldPath? path,
            [NotNullWhen(true)] out Type? type)
        {
            // if expression an interface conversion...
            if (expression is UnaryExpression uexpr && uexpr.NodeType == ExpressionType.Convert)
            {
                return TryResolveSubpath(uexpr.Operand, document, propertyPath, out path, out type);
            }

            // if expression is a property of the known entity.
            if (expression is MemberExpression mexpr
                && mexpr.Expression is not null
                && mexpr.Member is PropertyInfo prop
                && prop.DeclaringType is not null
                && Model.TryGetDataEntity(prop.DeclaringType, out var entity)
                && entity.Properties.TryGetFirst(d => d.Property.Equals(prop), out var pdata))
            {
                return TryResolveSubpath(mexpr.Expression, document, propertyPath.Add(pdata.Name), out path, out type);
            }
            // if expression is firestore field access
            if (expression is FirestoreFieldExpression fieldExpression && fieldExpression.Instance.Equals(document))
            {
                if (fieldExpression.RawPath is null)
                {
                    throw new InvalidOperationException("Special paths cannot be chained.");
                }
                path = propertyPath.ToFieldPath(fieldExpression.RawPath);
                type = fieldExpression.Type;
                return true;
            }
            path = default;
            type = default;
            return false;
        }

        /// <summary>
        /// Attempts to resolve field path for the expression. <paramref name="expression" /> must be chained to the
        /// initial query selector!
        /// </summary>
        /// <param name="expression">Simplified chained expression.</param>
        /// <param name="document">Root expression of the simplified chained Expression.</param>
        /// <param name="path">On success stores document relative path.</param>
        /// <param name="type">On success stores effective CLR type of the resolved member.</param>
        /// <returns></returns>
        protected bool TryResolvePath(
            Expression expression,
            ParameterExpression document,
            [NotNullWhen(true)] out FieldPath? path,
            [NotNullWhen(true)] out Type? type)
        {
            // simple case --> direct field.
            if (expression is FirestoreFieldExpression fieldExpression && fieldExpression.Instance.Equals(document))
            {
                path = fieldExpression.Path;
                type = fieldExpression.Type;
                return true;
            }
            // complex case --> property of subobject.
            #pragma warning disable CS0618
            return TryResolveSubpath(expression, document, ImmutableList<string>.Empty, out path, out type);
            #pragma warning restore CS0618
        }
    }
}