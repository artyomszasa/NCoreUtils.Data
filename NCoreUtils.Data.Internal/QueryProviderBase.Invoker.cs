using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace NCoreUtils.Data.Internal
{
    public partial class QueryProviderBase
    {
        private static class Invoker
        {
            private static readonly ConcurrentDictionary<Type, OneArgInvoker> _cache1 = new();

            private static readonly ConcurrentDictionary<(Type, Type), TwoArgsInvoker> _cache2 = new();

            private static readonly Func<Type, OneArgInvoker> _factory1 = CreateOneArgInvoker;

            private static readonly Func<(Type, Type), TwoArgsInvoker> _factory2 = CreateTwoArgInvoker;

            [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Type arguments from expressions should already be preserved.")]
            [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "Type arguments from expressions should already be preserved.")]
            private static OneArgInvoker CreateOneArgInvoker(Type type)
                => (OneArgInvoker)Activator.CreateInstance(typeof(OneArgInvoker<>).MakeGenericType(type))!;

            [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Type arguments from expressions should already be preserved.")]
            [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "Type arguments from expressions should already be preserved.")]
            private static TwoArgsInvoker CreateTwoArgInvoker((Type, Type) types)
                => (TwoArgsInvoker)Activator.CreateInstance(typeof(TwoArgsInvoker<,>).MakeGenericType(types.Item1, types.Item2))!;

            private static (Type, Type) GetSelectorTypes(Expression expression)
            {
                var gargs = expression.Type.GetGenericArguments();
                return (gargs[0], gargs[^1]);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool RegisterInvoker<TArg>()
                => _cache1.TryAdd(typeof(TArg), new OneArgInvoker<TArg>());

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool RegisterInvoker<TArg1, TArg2>()
                => _cache2.TryAdd((typeof(TArg1), typeof(TArg2)), new TwoArgsInvoker<TArg1, TArg2>());

            [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(TwoArgsInvoker<,>))]
            public static IQueryable ApplyOfType(QueryProviderBase provider, IQueryable source, Type targetType)
            {
                var key = (source.ElementType, targetType);
                return _cache2.GetOrAdd(key, _factory2)
                    .DoApplyOfType(provider, source);
            }

            [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(TwoArgsInvoker<,>))]
            public static IQueryable ApplyOrderBy(QueryProviderBase provider, IQueryable source, Expression selector)
            {
                var key = GetSelectorTypes(selector);
                return _cache2.GetOrAdd(key, _factory2)
                    .DoApplyOrderBy(provider, source, selector);
            }

            [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(TwoArgsInvoker<,>))]
            public static IQueryable ApplyOrderByDescending(QueryProviderBase provider, IQueryable source, Expression selector)
            {
                var key = GetSelectorTypes(selector);
                return _cache2.GetOrAdd(key, _factory2)
                    .DoApplyOrderByDescending(provider, source, selector);
            }

            [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(TwoArgsInvoker<,>))]
            public static IQueryable ApplySelectIndexed(QueryProviderBase provider, IQueryable source, Expression selector)
            {
                var key = GetSelectorTypes(selector);
                return _cache2.GetOrAdd(key, _factory2)
                    .DoApplySelectIndexed(provider, source, selector);
            }

            [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(TwoArgsInvoker<,>))]
            public static IQueryable ApplySelectUnindexed(QueryProviderBase provider, IQueryable source, Expression selector)
            {
                var key = GetSelectorTypes(selector);
                return _cache2.GetOrAdd(key, _factory2)
                    .DoApplySelectUnindexed(provider, source, selector);
            }

            [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(OneArgInvoker<>))]
            public static IQueryable ApplySkip(QueryProviderBase provider, IQueryable source, int count)
                => _cache1.GetOrAdd(source.ElementType, _factory1)
                    .DoApplySkip(provider, source, count);

            [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(OneArgInvoker<>))]
            public static IQueryable ApplyTake(QueryProviderBase provider, IQueryable source, int count)
                => _cache1.GetOrAdd(source.ElementType, _factory1)
                    .DoApplyTake(provider, source, count);

            [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(TwoArgsInvoker<,>))]
            public static IQueryable ApplyThenBy(QueryProviderBase provider, IQueryable source, Expression selector)
            {
                var key = GetSelectorTypes(selector);
                return _cache2.GetOrAdd(key, _factory2)
                    .DoApplyThenBy(provider, source, selector);
            }

            [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(TwoArgsInvoker<,>))]
            public static IQueryable ApplyThenByDescending(QueryProviderBase provider, IQueryable source, Expression selector)
            {
                var key = GetSelectorTypes(selector);
                return _cache2.GetOrAdd(key, _factory2)
                    .DoApplyThenByDescending(provider, source, selector);
            }

            [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(OneArgInvoker<>))]
            public static IQueryable ApplyWhereIndexed(QueryProviderBase provider, IQueryable source, Expression predicate)
                => _cache1.GetOrAdd(source.ElementType, _factory1)
                    .DoApplyWhereUnindexed(provider, source, predicate);

            [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(OneArgInvoker<>))]
            public static IQueryable ApplyWhereUnindexed(QueryProviderBase provider, IQueryable source, Expression predicate)
                => _cache1.GetOrAdd(source.ElementType, _factory1)
                    .DoApplyWhereUnindexed(provider, source, predicate);

            [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(OneArgInvoker<>))]
            public static Task<bool> ExecuteAll(QueryProviderBase provider, IQueryable source, Expression predicate, CancellationToken cancellationToken)
                => _cache1.GetOrAdd(source.ElementType, _factory1)
                    .DoExecuteAll(provider, source, predicate, cancellationToken);

            [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(OneArgInvoker<>))]
            public static Task<bool> ExecuteAny(QueryProviderBase provider, IQueryable source, CancellationToken cancellationToken)
                => _cache1.GetOrAdd(source.ElementType, _factory1)
                    .DoExecuteAny(provider, source, cancellationToken);

            [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(OneArgInvoker<>))]
            public static Task<int> ExecuteCount(QueryProviderBase provider, IQueryable source, CancellationToken cancellationToken)
                => _cache1.GetOrAdd(source.ElementType, _factory1)
                    .DoExecuteCount(provider, source, cancellationToken);
        }

        abstract class OneArgInvoker
        {
            public abstract IQueryable DoApplySkip(QueryProviderBase provider, IQueryable source, int count);

            public abstract IQueryable DoApplyTake(QueryProviderBase provider, IQueryable source, int count);

            public abstract IQueryable DoApplyWhereIndexed(QueryProviderBase provider, IQueryable source, Expression predicate);

            public abstract IQueryable DoApplyWhereUnindexed(QueryProviderBase provider, IQueryable source, Expression predicate);

            public abstract Task<bool> DoExecuteAll(QueryProviderBase provider, IQueryable source, Expression predicate, CancellationToken cancellationToken);

            public abstract Task<bool> DoExecuteAny(QueryProviderBase provider, IQueryable source, CancellationToken cancellationToken);

            public abstract Task<int> DoExecuteCount(QueryProviderBase provider, IQueryable source, CancellationToken cancellationToken);
        }

        sealed class OneArgInvoker<T> : OneArgInvoker
        {
            public override IQueryable DoApplySkip(QueryProviderBase provider, IQueryable source, int count)
            {
                var src = (IQueryable<T>)source;
                return provider.ApplySkip(src, count);
            }

            public override IQueryable DoApplyTake(QueryProviderBase provider, IQueryable source, int count)
            {
                var src = (IQueryable<T>)source;
                return provider.ApplyTake(src, count);
            }

            public override IQueryable DoApplyWhereIndexed(QueryProviderBase provider, IQueryable source, Expression predicate)
            {
                var src = (IQueryable<T>)source;
                var pred = (Expression<Func<T, int, bool>>)predicate;
                return provider.ApplyWhere(src, pred);
            }

            public override IQueryable DoApplyWhereUnindexed(QueryProviderBase provider, IQueryable source, Expression predicate)
            {
                var src = (IQueryable<T>)source;
                var pred = (Expression<Func<T, bool>>)predicate;
                return provider.ApplyWhere(src, pred);
            }

            public override Task<bool> DoExecuteAll(QueryProviderBase provider, IQueryable source, Expression predicate, CancellationToken cancellationToken)
            {
                var src = (IQueryable<T>)source;
                var pred = (Expression<Func<T, bool>>)predicate;
                return provider.ExecuteAll(src, pred, cancellationToken);
            }

            public override Task<bool> DoExecuteAny(QueryProviderBase provider, IQueryable source, CancellationToken cancellationToken)
            {
                var src = (IQueryable<T>)source;
                return provider.ExecuteAny(src, cancellationToken);
            }

            public override Task<int> DoExecuteCount(QueryProviderBase provider, IQueryable source, CancellationToken cancellationToken)
            {
                var src = (IQueryable<T>)source;
                return provider.ExecuteCount(src, cancellationToken);
            }
        }

        abstract class TwoArgsInvoker
        {
            public abstract IQueryable DoApplyOfType(QueryProviderBase provider, IQueryable source);

            public abstract IOrderedQueryable DoApplyOrderBy(QueryProviderBase provider, IQueryable source, Expression selector);

            public abstract IOrderedQueryable DoApplyOrderByDescending(QueryProviderBase provider, IQueryable source, Expression selector);

            public abstract IQueryable DoApplySelectIndexed(QueryProviderBase provider, IQueryable source, Expression selector);

            public abstract IQueryable DoApplySelectUnindexed(QueryProviderBase provider, IQueryable source, Expression selector);

            public abstract IOrderedQueryable DoApplyThenBy(QueryProviderBase provider, IQueryable source, Expression selector);

            public abstract IOrderedQueryable DoApplyThenByDescending(QueryProviderBase provider, IQueryable source, Expression selector);
        }

        sealed class TwoArgsInvoker<TSource, TResult> : TwoArgsInvoker
        {
            public override IQueryable DoApplyOfType(QueryProviderBase provider, IQueryable source)
            {
                var src = (IQueryable<TSource>)source;
                return provider.ApplyOfType<TSource, TResult>(src);
            }

            public override IOrderedQueryable DoApplyOrderBy(QueryProviderBase provider, IQueryable source, Expression selector)
            {
                var src = (IQueryable<TSource>)source;
                var sel = (Expression<Func<TSource, TResult>>)selector;
                return provider.ApplyOrderBy(src, sel);
            }

            public override IOrderedQueryable DoApplyOrderByDescending(QueryProviderBase provider, IQueryable source, Expression selector)
            {
                var src = (IQueryable<TSource>)source;
                var sel = (Expression<Func<TSource, TResult>>)selector;
                return provider.ApplyOrderByDescending(src, sel);
            }

            public override IQueryable DoApplySelectIndexed(QueryProviderBase provider, IQueryable source, Expression selector)
            {
                var src = (IQueryable<TSource>)source;
                var sel = (Expression<Func<TSource, int, TResult>>)selector;
                return provider.ApplySelect(src, sel);
            }

            public override IQueryable DoApplySelectUnindexed(QueryProviderBase provider, IQueryable source, Expression selector)
            {
                var src = (IQueryable<TSource>)source;
                var sel = (Expression<Func<TSource, TResult>>)selector;
                return provider.ApplySelect(src, sel);
            }

            public override IOrderedQueryable DoApplyThenBy(QueryProviderBase provider, IQueryable source, Expression selector)
            {
                var src = (IQueryable<TSource>)source;
                var sel = (Expression<Func<TSource, TResult>>)selector;
                return provider.ApplyThenBy(src, sel);
            }

            public override IOrderedQueryable DoApplyThenByDescending(QueryProviderBase provider, IQueryable source, Expression selector)
            {
                var src = (IQueryable<TSource>)source;
                var sel = (Expression<Func<TSource, TResult>>)selector;
                return provider.ApplyThenByDescending(src, sel);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool RegisterInvoker<TArg>() => Invoker.RegisterInvoker<TArg>();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool RegisterInvoker<TArg1, TArg2>() => Invoker.RegisterInvoker<TArg1, TArg2>();
    }
}