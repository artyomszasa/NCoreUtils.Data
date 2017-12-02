using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace NCoreUtils.Data
{
    /// <summary>
    /// Contains expression manupulation extensions.
    /// </summary>
    public static class ExpressionExtensions
    {
        sealed class ParameterSubstitution : ExpressionVisitor
        {
            readonly ParameterExpression _parameter;

            readonly Expression _replacement;

            public ParameterSubstitution(ParameterExpression parameter, Expression replacement)
            {
                _parameter = parameter;
                _replacement = replacement;
            }

            protected override Expression VisitParameter(ParameterExpression node)
            {
                if (node.Equals(_parameter))
                {
                    return this.Visit(_replacement);
                }
                return base.VisitParameter(node);
            }
        }

        /// <summary>
        /// Replaces all <paramref name="parameter" /> occurrences within the source expression with the specified
        /// expression.
        /// </summary>
        /// <typeparam name="T">Static type of the source expression.</typeparam>
        /// <param name="expression">Source expression.</param>
        /// <param name="parameter">Parameter expression to replace.</param>
        /// <param name="replacement">Replacement expression.</param>
        /// <returns>Result expression.</returns>
        public static T SubstituteParameter<T>(this T expression, ParameterExpression parameter, Expression replacement)
            where T : Expression
        {
            if (parameter == null)
            {
                throw new System.ArgumentNullException(nameof(parameter));
            }
            if (replacement == null)
            {
                throw new System.ArgumentNullException(nameof(replacement));
            }
            var visitor = new ParameterSubstitution(parameter, replacement);
            return (T)visitor.Visit(expression);
        }

        static Maybe<object> MaybeExtractConstantImpl(Expression source)
        {
            if (source is ConstantExpression constantExpression)
            {
                return constantExpression.Value.Just();
            }
            if (source is MemberExpression memberExpression)
            {
                switch (memberExpression.Member)
                {
                    case FieldInfo field:
                        if (field.IsStatic)
                        {
                            return field.GetValue(null).Just();
                        }
                        return MaybeExtractConstantImpl(memberExpression.Expression).Map(field.GetValue);
                    case PropertyInfo property when property.CanRead && null != property.GetMethod:
                        if (property.GetMethod.IsStatic)
                        {
                            return property.GetValue(null, null).Just();
                        }
                        return MaybeExtractConstantImpl(memberExpression.Expression).Map(instance => property.GetValue(instance, null));
                }
            }
            return Maybe.Nothing;
        }

        /// <summary>
        /// Exracts constant expression value as nullable.
        /// </summary>
        /// <param name="source">Source expression.</param>
        /// <returns>
        /// Either constant value of the epxression or empty value.
        /// </returns>
        public static Maybe<object> MaybeExtractConstant(this Expression source)
        {
            if (source == null)
            {
                throw new System.ArgumentNullException(nameof(source));
            }
            try
            {
                return MaybeExtractConstantImpl(source);
            }
            catch
            {
                return Maybe.Nothing;
            }
        }

        /// <summary>
        /// Attempts to extract constant value from the expression.
        /// </summary>
        /// <param name="source">Source expression.</param>
        /// <param name="value">Variable to store the extracted constant value.</param>
        /// <returns>
        /// <c>true</c> if constant value has been successfully extracted from the expression and stored into
        /// <paramref name="value" />, <c>false</c> otherwise.
        /// </returns>
        public static bool TryExtractConstant(this Expression source, out object value)
            => source.MaybeExtractConstant().TryGetValue(out value);

        /// <summary>
        /// Extracts method call data as nullable tuple.
        /// </summary>
        /// <param name="source">Source expression</param>
        /// <returns>
        /// Either method call data as tuple or empty value.
        /// </returns>
        public static Maybe<(MethodInfo method, Expression instance, ReadOnlyCollection<Expression> arguments)> MaybeExtractCall(this Expression source)
        {
            if (source is MethodCallExpression callExpression)
            {
                return (callExpression.Method, callExpression.Object, callExpression.Arguments).Just();
            }
            return Maybe.Nothing;
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