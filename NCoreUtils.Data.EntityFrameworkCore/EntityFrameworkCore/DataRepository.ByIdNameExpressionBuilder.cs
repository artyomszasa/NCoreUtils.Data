using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;

namespace NCoreUtils.Data.EntityFrameworkCore
{
    public partial class DataRepository
    {
        protected abstract class ByIdNameExpressionBuilder
        {
            static readonly ConcurrentDictionary<Type, ByIdNameExpressionBuilder> _cache = new ConcurrentDictionary<Type, ByIdNameExpressionBuilder>();

            static readonly Func<Type, ByIdNameExpressionBuilder> _createBuilder = entityType =>
            {
                if (typeof(IHasIdName).IsAssignableFrom(entityType))
                {
                    return (ByIdNameExpressionBuilder)Activator.CreateInstance(typeof(ByIdNameExpressionBuilder<>).MakeGenericType(entityType), true);
                }
                return NothingByIdNameExpressionBuilder.Instance;
            };

            public static Maybe<Expression> MaybeGetExpression(Type entitType)
                => _cache.GetOrAdd(entitType, _createBuilder)
                    .MaybeGetExpression();

            protected abstract Maybe<Expression> MaybeGetExpression();
        }

        sealed class NothingByIdNameExpressionBuilder : ByIdNameExpressionBuilder
        {
            internal static NothingByIdNameExpressionBuilder Instance { [ExcludeFromCodeCoverage] get; }

            [ExcludeFromCodeCoverage]
            static NothingByIdNameExpressionBuilder() => Instance = new NothingByIdNameExpressionBuilder();

            [ExcludeFromCodeCoverage]
            NothingByIdNameExpressionBuilder() { }

            [ExcludeFromCodeCoverage]
            protected override Maybe<Expression> MaybeGetExpression() => Maybe.Nothing;
        }

        sealed class ByIdNameExpressionBuilder<T> : ByIdNameExpressionBuilder
            where T : IHasIdName
        {
            protected override Maybe<Expression> MaybeGetExpression()
            {
                Expression expr = LinqExtensions.ReplaceExplicitProperties<Func<T, string>>(e => e.IdName);
                return expr.Just();
            }
        }
    }
}