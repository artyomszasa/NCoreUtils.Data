using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using Google.Cloud.Firestore;

namespace NCoreUtils.Data.Google.Cloud.Firestore
{
    public partial class FirestoreQueryProvider
    {
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
    }
}