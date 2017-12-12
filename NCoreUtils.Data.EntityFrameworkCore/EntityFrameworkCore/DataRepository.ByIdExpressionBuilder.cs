using System;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using NCoreUtils.Linq;

namespace NCoreUtils.Data.EntityFrameworkCore
{
    partial class DataRepository
    {
        static class ByIdExpressionBuilder
        {
            public static ConcurrentDictionary<Type, LambdaExpression> IdAccessors { get; } = new ConcurrentDictionary<Type, LambdaExpression>();
        }

        protected static class ByIdExpressionBuilder<TData, TId>
            where TData : IHasId<TId>
        {
            static Expression<Func<TData, TId>> GetIdProperty()
            {
                Expression<Func<TData, TId>> expression = entity => entity.Id;
                return LinqExtensions.ReplaceExplicitProperties(expression);
            }

            public static Expression<Func<TData, bool>> CreateFilter(TId id)
            {
                if (!ByIdExpressionBuilder.IdAccessors.TryGetValue(typeof(TData), out var idAccessor))
                {
                    idAccessor = ByIdExpressionBuilder.IdAccessors.GetOrAdd(typeof(TData), _ => GetIdProperty());
                }
                var parameterExpression = Expression.Parameter(typeof(TData));
                return Expression.Lambda<Func<TData, bool>>(
                    Expression.Equal(
                        idAccessor.Body.SubstituteParameter(idAccessor.Parameters[0], parameterExpression),
                        Expression.Constant(id)
                    ),
                    parameterExpression);
            }
        }
    }
}