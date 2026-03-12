
using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;

namespace NCoreUtils.Data.Internal;

public partial class QueryProviderBase
{
    private abstract class ExecuteEnumerableInvoker
    {
        private static readonly LookupDictionary<Type, ExecuteEnumerableInvoker> _cache = new();

        [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "Only preserved types should be passed.")]
        private static readonly Func<Type, ExecuteEnumerableInvoker> _createInvoker = elementType
            => (ExecuteEnumerableInvoker)Activator.CreateInstance(
                typeof(ExecuteEnumerableInvoker<>).MakeGenericType(elementType)
            )!;

        public static IEnumerable ExecuteEnumerable(QueryProviderBase provider, Type elementType, Expression expression)
            => _cache.GetOrAdd(elementType, _createInvoker)
                .DoExecuteEnumerable(provider, expression);

        protected abstract IEnumerable DoExecuteEnumerable(QueryProviderBase provider, Expression expression);
    }

    private sealed class ExecuteEnumerableInvoker<TElement>
        : ExecuteEnumerableInvoker
    {
        protected override IEnumerable DoExecuteEnumerable(QueryProviderBase provider, Expression expression)
            => provider.ExecuteEnumerable<TElement>(expression);
    }
}