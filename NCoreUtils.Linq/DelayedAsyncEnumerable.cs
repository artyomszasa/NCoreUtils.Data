using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace NCoreUtils.Linq
{
    sealed class DelayedAsyncEnumerable<T> : IAsyncEnumerable<T>
    {
        readonly Func<CancellationToken, Task<IAsyncEnumerable<T>>> _factory;

        public DelayedAsyncEnumerable(Func<CancellationToken, Task<IAsyncEnumerable<T>>> factory)
            => _factory = factory;

        public IAsyncEnumerator<T> GetEnumerator() => new DelayedAsyncEnumerator<T>(async cancellationToken => (await _factory(cancellationToken)).GetEnumerator());
    }

    public static class DelayedAsyncEnumerable
    {
        public static IAsyncEnumerable<T> Delay<T>(Func<CancellationToken, Task<IAsyncEnumerable<T>>> factory)
            => new DelayedAsyncEnumerable<T>(factory);
    }
}