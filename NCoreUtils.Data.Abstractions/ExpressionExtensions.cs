using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using NCoreUtils.Linq;

namespace NCoreUtils.Data
{
    /// <summary>
    /// Contains expression manupulation extensions.
    /// </summary>
    public static class ExpressionExtensions
    {
        static Maybe<PropertyInfo> MaybeExtractBodyProperty(this Expression body)
        {
            if (body is MemberExpression memberExpression && memberExpression.Member is PropertyInfo propertyInfo)
            {
                return propertyInfo.Just();
            }
            if (body.NodeType == ExpressionType.Convert)
            {
                return ((UnaryExpression)body).Operand.MaybeExtractBodyProperty();
            }
            return Maybe.Nothing;
        }

        /// <summary>
        /// Extracts property info from the expression.
        /// </summary>
        /// <param name="expression">Source expression.</param>
        /// <returns>Either property info or empty value.</returns>
        public static Maybe<PropertyInfo> MaybeExtractProperty(this LambdaExpression expression)
        {
            if (expression == null)
            {
                throw new ArgumentNullException(nameof(expression));
            }
            return expression.Body.MaybeExtractBodyProperty();
            //throw new InvalidOperationException($"Expected property access expression, got: {expression}.");
        }

        /// <summary>
        /// Extracts property info from the expression.
        /// </summary>
        /// <param name="expression">Source expression.</param>
        /// <param name="propertyInfo">On success contains extracted property.</param>
        /// <returns><c>true</c> if property info could be extracted from expression, <c>false</c> otherwise.</returns>
        public static bool TryExtractProperty(this LambdaExpression expression, out PropertyInfo propertyInfo)
            => MaybeExtractProperty(expression).TryGetValue(out propertyInfo);

        /// <summary>
        /// Extracts property info from the expression.
        /// </summary>
        /// <param name="expression">Source expression.</param>
        /// <returns>Extracted property info.</returns>
        /// <exception cref="System.InvalidOperationException">
        /// Thrown if property info cannot be extracted from the specified expression.
        /// </exception>
        public static PropertyInfo ExtractProperty(this LambdaExpression expression)
        {
            if (TryExtractProperty(expression, out var propertyInfo))
            {
                return propertyInfo;
            }
            throw new InvalidOperationException($"Expected property access expression, got: {expression}.");
        }

        /// <summary>
        /// Extracts query object from expression as nullable value.
        /// </summary>
        /// <param name="source">Source expression.</param>
        /// <param name="extensionTypes">Optional extensions types to accept.</param>
        /// <returns>
        /// Either query object or empty value.
        /// </returns>
        public static Maybe<IQueryable> MaybeExtractQueryable(this Expression source, params Type[] extensionTypes)
        {
            if (source == null)
            {
                throw new System.ArgumentNullException(nameof(source));
            }
            // try extract constant
            return source.MaybeExtractConstant()
                .As<IQueryable>()
                .Supply(() =>
                    // try extract queryable call argument
                    source.MaybeExtractCall()
                        .Where(m => m.method.IsStatic && (m.method.DeclaringType == typeof(Queryable) || Array.IndexOf(extensionTypes, m.method.DeclaringType) >= 0) && m.arguments.Count > 0)
                        .Map(tup => tup.arguments[0])
                        .Bind(arg => MaybeExtractQueryable(arg, extensionTypes)));
        }

        /// <summary>
        /// Attempts to extract query object from the expression.
        /// </summary>
        /// <param name="source">Source expression.</param>
        /// <param name="queryable">Variable to store query object.</param>
        /// <param name="extensionTypes">Optional extensions types to accept.</param>
        /// <returns>
        /// <c>true</c> if query object has been successfully extracted from the expression and stored into
        /// <paramref name="queryable" />, <c>false</c> otherwise.
        /// </returns>
        public static bool TryExtractQueryable(this Expression source, out IQueryable queryable, params Type[] extensionTypes)
            => source.MaybeExtractQueryable(extensionTypes).TryGetValue(out queryable);

    }
}