using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace NCoreUtils.Linq
{
    sealed class AsyncSelectEnumerator<TSource, TResult> : IAsyncEnumerator<TResult>
    {
        public IAsyncEnumerator<TSource> Source { get; }

        public Func<TSource, CancellationToken, Task<TResult>> Selector { get; }

        public TResult Current { get; private set; }

        public AsyncSelectEnumerator(IAsyncEnumerator<TSource> source, Func<TSource, CancellationToken, Task<TResult>> selector)
        {
            Source = source;
            Selector = selector;
        }

        public void Dispose() => Source.Dispose();

        public async Task<bool> MoveNext(CancellationToken cancellationToken)
        {
            if (await Source.MoveNext(cancellationToken).ConfigureAwait(false))
            {
                Current = await Selector(Source.Current, cancellationToken).ConfigureAwait(false);
                return true;
            }
            Current = default;
            return false;
        }
    }
}