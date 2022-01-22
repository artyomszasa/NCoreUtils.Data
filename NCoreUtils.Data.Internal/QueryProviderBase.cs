using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using NCoreUtils.Linq;

namespace NCoreUtils.Data.Internal
{
    public abstract partial class QueryProviderBase : IQueryProvider, IAsyncQueryProvider
    {
        static readonly MethodInfo _gmExecute;

        // static readonly MethodInfo _gmExecuteEnumerableAsync;

        static readonly MethodInfo _gmExecuteEnumerable;

        [UnconditionalSuppressMessage("Trimming", "IL2111", Justification = "Generic mezhod only used on preserved types.")]
        static QueryProviderBase()
        {
            var methods = typeof(QueryProviderBase).GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            _gmExecute = methods.First(m => m.IsGenericMethodDefinition && m.Name == nameof(Execute));
            // _gmExecuteEnumerableAsync = methods.First(m => m.IsGenericMethodDefinition && m.Name == nameof(ExecuteEnumerableAsync));
            _gmExecuteEnumerable = methods.First(m => m.IsGenericMethodDefinition && m.Name == nameof(ExecuteEnumerable));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static T ReBox<T>(object source) => (T)source;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static Expression Unquote(Expression expression)
        {
            if (expression is UnaryExpression unary && unary.NodeType == ExpressionType.Quote)
            {
                return Unquote(unary.Operand);
            }
            return expression;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool IsIndexedFunc(IReadOnlyList<Expression> args)
        {
            if (args.Count < 1)
            {
                return false;
            }
            var arg = Unquote(args[0]);
            var argType = arg.Type;
            return argType.IsGenericType && argType.GetGenericTypeDefinition().Equals(typeof(Func<,,>));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool IsUnindexedFunc(IReadOnlyList<Expression> args)
        {
            if (args.Count < 1)
            {
                return false;
            }
            var arg = Unquote(args[0]);
            var argType = arg.Type;
            return argType.IsGenericType && argType.GetGenericTypeDefinition().Equals(typeof(Func<,>));
        }

        static bool TryGetEnumerableElementType([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] Type type, [NotNullWhen(true)] out Type? elementType)
        {
            if (TryInterface(type, out elementType))
            {
                return true;
            }
            foreach (var itype in type.GetInterfaces())
            {
                if (TryInterface(itype, out elementType))
                {
                    return true;
                }
            }
            return false;

            static bool TryInterface(Type type, out Type elementType)
            {
                if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                {
                    elementType = type.GetGenericArguments()[0];
                    return true;
                }
                elementType = default!;
                return false;
            }
        }

        IEnumerable<T> ExecuteEnumerable<T>(Expression expression)
            => ExecuteEnumerableAsync<T>(expression).ToEnumerable();

        protected abstract IQueryable<TResult> ApplyOfType<TElement, TResult>(IQueryable<TElement> source);

        protected abstract IOrderedQueryable<TElement> ApplyOrderBy<TElement, TKey>(IQueryable<TElement> source, Expression<Func<TElement, TKey>> selector);

        protected abstract IOrderedQueryable<TElement> ApplyOrderByDescending<TElement, TKey>(IQueryable<TElement> source, Expression<Func<TElement, TKey>> selector);

        protected abstract IQueryable<TResult> ApplySelect<TSource, TResult>(IQueryable<TSource> source, Expression<Func<TSource, TResult>> selector);

        protected abstract IQueryable<TResult> ApplySelect<TSource, TResult>(IQueryable<TSource> source, Expression<Func<TSource, int, TResult>> selector);

        protected abstract IQueryable<TElement> ApplySkip<TElement>(IQueryable<TElement> source, int count);

        protected abstract IQueryable<TElement> ApplyTake<TElement>(IQueryable<TElement> source, int count);

        protected abstract IOrderedQueryable<TElement> ApplyThenBy<TElement, TKey>(IQueryable<TElement> source, Expression<Func<TElement, TKey>> selector);

        protected abstract IOrderedQueryable<TElement> ApplyThenByDescending<TElement, TKey>(IQueryable<TElement> source, Expression<Func<TElement, TKey>> selector);

        protected abstract IQueryable<TElement> ApplyWhere<TElement>(IQueryable<TElement> source, Expression<Func<TElement, bool>> predicate);

        protected abstract IQueryable<TElement> ApplyWhere<TElement>(IQueryable<TElement> source, Expression<Func<TElement, int, bool>> predicate);

        protected abstract Task<bool> ExecuteAll<TElement>(IQueryable<TElement> source, Expression<Func<TElement, bool>> predicate, CancellationToken cancellationToken);

        protected abstract Task<bool> ExecuteAny<TElement>(IQueryable<TElement> source, CancellationToken cancellationToken);

        protected abstract Task<int> ExecuteCount<TElement>(IQueryable<TElement> source, CancellationToken cancellationToken);

        protected abstract Task<TElement> ExecuteFirst<TElement>(IQueryable<TElement> source, CancellationToken cancellationToken);

        protected abstract Task<TElement> ExecuteFirstOrDefault<TElement>(IQueryable<TElement> source, CancellationToken cancellationToken);

        protected abstract Task<TElement> ExecuteLast<TElement>(IQueryable<TElement> source, CancellationToken cancellationToken);

        protected abstract Task<TElement> ExecuteLastOrDefault<TElement>(IQueryable<TElement> source, CancellationToken cancellationToken);

        protected abstract Task<TElement> ExecuteSingle<TElement>(IQueryable<TElement> source, CancellationToken cancellationToken);

        protected abstract Task<TElement> ExecuteSingleOrDefault<TElement>(IQueryable<TElement> source, CancellationToken cancellationToken);

        protected abstract IAsyncEnumerable<TElement> ExecuteQuery<TElement>(IQueryable<TElement> source);

        protected virtual bool TryExtractQueryableCallArguments(
            MethodCallExpression expression,
            [NotNullWhen(true)] out IQueryable? source,
            [NotNullWhen(true)] out IReadOnlyList<Expression>? arguments)
        {
            if (expression.Method.DeclaringType is not null && expression.Method.DeclaringType.Equals(typeof(Queryable))
                && expression.Arguments.Count > 0 && expression.Arguments[0].TryExtractQueryable(out var queryable))
            {
                source = queryable;
                var args = new List<Expression>(expression.Arguments.Count - 1);
                for (var i = 1; i < expression.Arguments.Count; ++i)
                {
                    args.Add(expression.Arguments[i]);
                }
                arguments = args;
                return true;
            }
            source = default;
            arguments = default;
            return false;
        }

        public virtual IQueryable CreateQuery(Expression expression)
        {
            if (expression is MethodCallExpression methodCallExpression && TryExtractQueryableCallArguments(methodCallExpression, out var source, out var arguments))
            {
                switch (methodCallExpression.Method)
                {
                    case MethodInfo { Name: nameof(Queryable.OfType) } m:
                        return Invoker.ApplyOfType(this, source, m.GetGenericArguments()[0]);
                    case { Name: nameof(Queryable.OrderBy) }:
                        return Invoker.ApplyOrderBy(this, source, Unquote(arguments[0]));
                    case { Name: nameof(Queryable.OrderByDescending) }:
                        return Invoker.ApplyOrderByDescending(this, source, Unquote(arguments[0]));
                    case { Name: nameof(Queryable.Select) } when (IsIndexedFunc(arguments)):
                        return Invoker.ApplySelectIndexed(this, source, Unquote(arguments[0]));
                    case { Name: nameof(Queryable.Select) } when (IsUnindexedFunc(arguments)):
                        return Invoker.ApplySelectUnindexed(this, source, Unquote(arguments[0]));
                    case { Name: nameof(Queryable.Skip) }:
                        return Invoker.ApplySkip(this, source, arguments[0].ExtractInt32());
                    case { Name: nameof(Queryable.Take) }:
                        return Invoker.ApplyTake(this, source, arguments[0].ExtractInt32());
                    case { Name: nameof(Queryable.ThenBy) }:
                        return Invoker.ApplyThenBy(this, source, Unquote(arguments[0]));
                    case { Name: nameof(Queryable.ThenByDescending) }:
                        return Invoker.ApplyThenByDescending(this, source, Unquote(arguments[0]));
                    case { Name: nameof(Queryable.Where) } when (IsIndexedFunc(arguments)):
                        return Invoker.ApplyWhereIndexed(this, source, Unquote(arguments[0]));
                    case { Name: nameof(Queryable.Where) } when (IsUnindexedFunc(arguments)):
                        return Invoker.ApplyWhereUnindexed(this, source, Unquote(arguments[0]));
                }
            }
            throw new NotSupportedException($"Not supported expression: {expression}.");
        }

        public virtual IQueryable<TElement> CreateQuery<TElement>(Expression expression)
        {
            return (IQueryable<TElement>)CreateQuery(expression);
        }

        [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Only preserved types should be handled here.")]
        [UnconditionalSuppressMessage("Trimming", "IL2060", Justification = "Only preserved types should be handled here.")]
        [DynamicDependency("Execute`1", typeof(QueryProviderBase))]
        public virtual object Execute(Expression expression)
        {
            Type resultType;
            if (expression.TryExtractQueryable(out var queryable))
            {
                resultType = typeof(IEnumerable<>).MakeGenericType(queryable.ElementType);
            }
            else
            {
                resultType = expression.Type;
            }
            return _gmExecute.MakeGenericMethod(resultType).Invoke(this, new object[] { expression })!;
        }

        [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Only preserved types should be handled here.")]
        [UnconditionalSuppressMessage("Trimming", "IL2060", Justification = "Only preserved types should be handled here.")]
        [UnconditionalSuppressMessage("Trimming", "IL2087", Justification = "If interface type is not preserved the function makes no sense anyway.")]
        [DynamicDependency("ExecuteEnumerable`1", typeof(QueryProviderBase))]
        public virtual TResult Execute<TResult>(
            Expression expression)
        {
            if (TryGetEnumerableElementType(typeof(TResult), out var elementType))
            {
                return (TResult)_gmExecuteEnumerable.MakeGenericMethod(elementType).Invoke(this, new object[] { expression })!;
            }
            return ExecuteAsync<TResult>(expression, CancellationToken.None).GetAwaiter().GetResult();
        }

        public IAsyncEnumerable<T> ExecuteEnumerableAsync<T>(Expression expression)
        {
            if (expression.TryExtractQueryable(out var queryable))
            {
                return ExecuteQuery((IQueryable<T>)queryable);
            }
            throw new InvalidOperationException($"Invalid query expression {expression}");
        }

        public async Task<T> ExecuteAsync<T>(Expression expression, CancellationToken cancellationToken)
        {
            if (expression is MethodCallExpression methodCallExpression && TryExtractQueryableCallArguments(methodCallExpression, out var source, out var arguments))
            {
                switch (methodCallExpression.Method)
                {
                    case { Name: nameof(Queryable.All) }:
                        return ReBox<T>(await Invoker.ExecuteAll(this, source, Unquote(arguments[0]), cancellationToken));
                    case { Name: nameof(Queryable.Any) } when (arguments.Count > 0):
                        return ReBox<T>(await Invoker.ExecuteAny(this, Invoker.ApplyWhereUnindexed(this, source, Unquote(arguments[0])), cancellationToken));
                    case { Name: nameof(Queryable.Any) }:
                        return ReBox<T>(await Invoker.ExecuteAny(this, source, cancellationToken));
                    case { Name: nameof(Queryable.Count) } when (arguments.Count > 0):
                        return ReBox<T>(await Invoker.ExecuteCount(this, Invoker.ApplyWhereUnindexed(this, source, Unquote(arguments[0])), cancellationToken));
                    case { Name: nameof(Queryable.Count) }:
                        return ReBox<T>(await Invoker.ExecuteCount(this, source, cancellationToken));
                    case { Name: nameof(Queryable.First) } when (arguments.Count > 0):
                        return await ExecuteFirst((IQueryable<T>)Invoker.ApplyWhereUnindexed(this, source, Unquote(arguments[0])), cancellationToken);
                    case { Name: nameof(Queryable.First) }:
                        return await ExecuteFirst((IQueryable<T>)source, cancellationToken);
                    case { Name: nameof(Queryable.FirstOrDefault) } when (arguments.Count > 0):
                        return await ExecuteFirstOrDefault((IQueryable<T>)Invoker.ApplyWhereUnindexed(this, source, Unquote(arguments[0])), cancellationToken);
                    case { Name: nameof(Queryable.FirstOrDefault) }:
                        return await ExecuteFirstOrDefault((IQueryable<T>)source, cancellationToken);
                    case { Name: nameof(Queryable.Last) } when (arguments.Count > 0):
                        return await ExecuteLast((IQueryable<T>)Invoker.ApplyWhereUnindexed(this, source, Unquote(arguments[0])), cancellationToken);
                    case { Name: nameof(Queryable.Last) }:
                        return await ExecuteLast((IQueryable<T>)source, cancellationToken);
                    case { Name: nameof(Queryable.LastOrDefault) } when (arguments.Count > 0):
                        return await ExecuteLastOrDefault((IQueryable<T>)Invoker.ApplyWhereUnindexed(this, source, Unquote(arguments[0])), cancellationToken);
                    case { Name: nameof(Queryable.LastOrDefault) }:
                        return await ExecuteLastOrDefault((IQueryable<T>)source, cancellationToken);
                    case { Name: nameof(Queryable.Single) } when (arguments.Count > 0):
                        return await ExecuteSingle((IQueryable<T>)Invoker.ApplyWhereUnindexed(this, source, Unquote(arguments[0])), cancellationToken);
                    case { Name: nameof(Queryable.Single) }:
                        return await ExecuteSingle((IQueryable<T>)source, cancellationToken);
                    case { Name: nameof(Queryable.SingleOrDefault) } when (arguments.Count > 0):
                        return await ExecuteSingleOrDefault((IQueryable<T>)Invoker.ApplyWhereUnindexed(this, source, Unquote(arguments[0])), cancellationToken);
                    case { Name: nameof(Queryable.SingleOrDefault) }:
                        return await ExecuteSingleOrDefault((IQueryable<T>)source, cancellationToken);
                }
            }
            throw new NotSupportedException($"Not supported expression: {expression}.");
        }
    }
}