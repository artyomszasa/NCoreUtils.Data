using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace NCoreUtils.Linq
{
    sealed class AsyncWhereEnumerator<T> : IAsyncEnumerator<T>
    {
        public IAsyncEnumerator<T> Source { get; }

        public Func<T, CancellationToken, Task<bool>> Predicate { get; }

        public T Current { get; private set; }

        public AsyncWhereEnumerator(IAsyncEnumerator<T> source, Func<T, CancellationToken, Task<bool>> predicate)
        {
            Source = source;
            Predicate = predicate;
        }

        public void Dispose() => Source.Dispose();

        public async Task<bool> MoveNext(CancellationToken cancellationToken)
        {
            while (await Source.MoveNext(cancellationToken).ConfigureAwait(false))
            {
                var current = Source.Current;
                if (await Predicate(current, cancellationToken).ConfigureAwait(false))
                {
                    Current = current;
                    return true;
                }
            }
            Current = default(T);
            return false;
        }
    }
}