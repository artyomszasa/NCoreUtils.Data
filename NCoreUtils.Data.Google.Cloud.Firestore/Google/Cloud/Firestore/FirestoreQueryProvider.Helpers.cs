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
            private static readonly EmptyAsyncEnumerator<T> _enumerator = new EmptyAsyncEnumerator<T>();

            public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
                => _enumerator;
        }

        [Obsolete("TryResolveSubpath(...) is an internal mathod, use TryResolvePath(...).")]
        private bool TryResolveSubpath(Expression expression, ParameterExpression document, ImmutableList<string> prefix, [NotNullWhen(true)] out FieldPath? path)
        {
            // if expression an interface conversion...
            if (expression is UnaryExpression uexpr && uexpr.NodeType == ExpressionType.Convert)
            {
                return TryResolveSubpath(uexpr.Operand, document, prefix, out path);
            }

            // if expression is a property of the known entity.
            if (expression is MemberExpression mexpr
                && !(mexpr.Expression is null)
                && mexpr.Member is PropertyInfo prop
                && Model.TryGetDataEntity(prop.DeclaringType, out var entity)
                && entity.Properties.TryGetFirst(d => d.Property.Equals(prop), out var pdata))
            {
                return TryResolveSubpath(mexpr.Expression, document, prefix.Add(pdata.Name), out path);
            }
            // if expression is firestore field access
            if (expression is FirestoreFieldExpression fieldExpression && fieldExpression.Instance.Equals(document))
            {
                if (fieldExpression.RawPath is null)
                {
                    throw new InvalidOperationException("Special paths cannot be chained.");
                }
                path = prefix.ToFieldPath(fieldExpression.RawPath);
                return true;
            }
            path = default;
            return false;
        }

        /// <summary>
        /// Attempts to resolve field path for the expression. <paramref name="expression" /> must be chained to the
        /// initial query selector!
        /// </summary>
        /// <param name="expression">Simplified chained expression.</param>
        /// <param name="document">Root expression of the simplified chained Expression.</param>
        /// <returns></returns>
        protected bool TryResolvePath(Expression expression, ParameterExpression document, [NotNullWhen(true)] out FieldPath? path)
        {
            // simple case --> direct field.
            if (expression is FirestoreFieldExpression fieldExpression && fieldExpression.Instance.Equals(document))
            {
                path = fieldExpression.Path;
                return true;
            }
            // complex case --> property of subobject.
            #pragma warning disable CS0618
            return TryResolveSubpath(expression, document, ImmutableList<string>.Empty, out path);
            #pragma warning restore CS0618
        }

        /*
        protected virtual FieldPath ResolvePath(Expression expression, ParameterExpression rootParameter)
        {
            // handle key expression
            if (expression is MemberExpression mexpr && mexpr.Member is PropertyInfo prop && mexpr.Expression.Equals(rootParameter))
            {
                if (!Model.TryGetDataEntity(rootParameter.Type, out var entity))
                {
                    throw new InvalidOperationException($"Unable to resolve path as type {rootParameter.Type} is not registered as entity.");
                }
                var key = entity.Key;
                if (null != key && key.Count == 1 && key[0].Equals(prop))
                {
                    return FieldPath.DocumentId;
                }
            }
            // FIXME: pool
            var segments = new List<string>(4);
            ResolvePathRec(Model, expression, rootParameter, segments);
            return new FieldPath(segments.ToArray());

            static void ResolvePathRec(FirestoreModel model, Expression expression, ParameterExpression rootParameter, List<string> path)
            {
                if (expression is ParameterExpression pexpr && pexpr == rootParameter)
                {
                    return;
                }
                if (expression is MemberExpression mexpr && mexpr.Member is PropertyInfo property)
                {
                    ResolvePathRec(model, mexpr.Expression, rootParameter, path);
                    if (!model.TryGetDataEntity(property.DeclaringType, out var entity))
                    {
                        throw new InvalidOperationException($"Unable to resolve path as type {property.DeclaringType} is not registered as entity.");
                    }
                    if (!entity.Properties.TryGetFirst(e => e.Property.Equals(property), out var prop))
                    {
                        throw new InvalidOperationException($"Unable to resolve path as no property definition found for {property.DeclaringType}.{property.Name}.");
                    }
                    path.Add(prop.Name);
                }
                throw new NotSupportedException($"Not supported expression {expression} while resolving property path.");
            }
        }
        */
    }
}