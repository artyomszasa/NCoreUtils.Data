using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace NCoreUtils.Linq
{
    sealed class AsyncWhereEnumerable<T> : IAsyncEnumerable<T>
    {
        public IAsyncEnumerable<T> Source { get; }

        public Func<T, CancellationToken, Task<bool>> Predicate { get; }

        public T Current { get; private set; }

        public AsyncWhereEnumerable(IAsyncEnumerable<T> source, Func<T, CancellationToken, Task<bool>> predicate)
        {
            Source = source;
            Predicate = predicate;
        }

        public IAsyncEnumerator<T> GetEnumerator() => new AsyncWhereEnumerator<T>(Source.GetEnumerator(), Predicate);
    }
}