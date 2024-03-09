using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using NCoreUtils.Linq;

#pragma warning disable IL2110, IL2111

namespace NCoreUtils.Data.EntityFrameworkCore
{
    partial class DataRepository
    {
        private sealed class IdBox<T>
        {
            public T Value;

            public IdBox(T value)
                => Value = value;
        }

        static class ByIdExpressionBuilder
        {
            public static ConcurrentDictionary<Type, LambdaExpression> IdAccessors { get; } = new ConcurrentDictionary<Type, LambdaExpression>();

            public static ConcurrentDictionary<Type, FieldInfo> IdBoxFieldCache { get; } = new ConcurrentDictionary<Type, FieldInfo>();
        }

        protected static class ByIdExpressionBuilder<TData, TId>
            where TData : IHasId<TId>
        {
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields
                | DynamicallyAccessedMemberTypes.PublicConstructors)]
            private static readonly Type _idBoxType = typeof(IdBox<TId>);


            private static Expression<Func<TData, TId>> GetIdProperty()
            {
                Expression<Func<TData, TId>> expression = entity => entity.Id;
                return LinqExtensions.ReplaceExplicitProperties(expression);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static FieldInfo GetIdBoxField([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)] Type ty)
                => ty.GetField(nameof(IdBox<int>.Value), BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)!;

            [UnconditionalSuppressMessage("Trimming", "IL2111", Justification = "IdBox<> type is preserved througs static field.")]
            public static Expression<Func<TData, bool>> CreateFilter(TId id)
            {
                if (!ByIdExpressionBuilder.IdAccessors.TryGetValue(typeof(TData), out var idAccessor))
                {
                    idAccessor = ByIdExpressionBuilder.IdAccessors.GetOrAdd(typeof(TData), _ => GetIdProperty());
                }
                // see: https://github.com/aspnet/EntityFrameworkCore/issues/8909
                // see: https://github.com/aspnet/EntityFrameworkCore/issues/10535
                // instead of constant member access expression must be generated to avoid cache issues.
                if (!ByIdExpressionBuilder.IdBoxFieldCache.TryGetValue(_idBoxType, out var idBoxField))
                {
                    idBoxField = ByIdExpressionBuilder.IdBoxFieldCache.GetOrAdd(_idBoxType, GetIdBoxField);
                }
                var idBox = new IdBox<TId>(id);
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

#pragma warning restore IL2110, IL2111