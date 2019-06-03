using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace NCoreUtils.Linq
{
    sealed class DelayedAsyncEnumerator<T> : IAsyncEnumerator<T>
    {
        readonly Func<CancellationToken, Task<IAsyncEnumerator<T>>> _factory;

        IAsyncEnumerator<T> _source;

        public DelayedAsyncEnumerator(Func<CancellationToken, Task<IAsyncEnumerator<T>>> factory)
            => _factory = factory;

        public T Current => null == _source ? default : _source.Current;

        public void Dispose() => _source?.Dispose();

        public async Task<bool> MoveNext(CancellationToken cancellationToken)
        {
            if (null == _source)
            {
                _source = await _factory(cancellationToken);
            }
            return await _source.MoveNext(cancellationToken);
        }
    }

    public static class DelayedAsyncEnumerator
    {
        public static IAsyncEnumerator<T> Delay<T>(Func<CancellationToken, Task<IAsyncEnumerator<T>>> factory)
            => new DelayedAsyncEnumerator<T>(factory);
    }
}