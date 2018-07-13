using System;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using NCoreUtils.Linq;

namespace NCoreUtils.Data.EntityFrameworkCore
{
    partial class DataRepository
    {
        sealed class IdBox<T>
        {
            public T Value;
        }

        static class ByIdExpressionBuilder
        {
            public static ConcurrentDictionary<Type, LambdaExpression> IdAccessors { get; } = new ConcurrentDictionary<Type, LambdaExpression>();

            public static ConcurrentDictionary<Type, FieldInfo> IdBoxFieldCache { get; } = new ConcurrentDictionary<Type, FieldInfo>();
        }

        protected static class ByIdExpressionBuilder<TData, TId>
            where TData : IHasId<TId>
        {
            static Expression<Func<TData, TId>> GetIdProperty()
            {
                Expression<Func<TData, TId>> expression = entity => entity.Id;
                return LinqExtensions.ReplaceExplicitProperties(expression);
            }

            static FieldInfo GetIdBoxField(Type ty) => ty.GetField(nameof(IdBox<int>.Value), BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

            public static Expression<Func<TData, bool>> CreateFilter(TId id)
            {
                if (!ByIdExpressionBuilder.IdAccessors.TryGetValue(typeof(TData), out var idAccessor))
                {
                    idAccessor = ByIdExpressionBuilder.IdAccessors.GetOrAdd(typeof(TData), _ => GetIdProperty());
                }
                // see: https://github.com/aspnet/EntityFrameworkCore/issues/8909
                // see: https://github.com/aspnet/EntityFrameworkCore/issues/10535
                // instead of constant member access expression must be generated to avoid cache issues.
                if (!ByIdExpressionBuilder.IdBoxFieldCache.TryGetValue(typeof(IdBox<TId>), out var idBoxField))
                {
                    idBoxField = ByIdExpressionBuilder.IdBoxFieldCache.GetOrAdd(typeof(IdBox<TId>), GetIdBoxField);
                }
                var idBox = new IdBox<TId> { Value = id };
                var idExpression = Expression.Field(Expression.Constant(idBox, typeof(IdBox<TId>)), idBoxField);
                var parameterExpression = Expression.Parameter(typeof(TData));
                return Expression.Lambda<Func<TData, bool>>(
                    Expression.Equal(
                        idAccessor.Body.SubstituteParameter(idAccessor.Parameters[0], parameterExpression),
                        idExpression
                    ),
                    parameterExpression);
            }
        }
    }
}