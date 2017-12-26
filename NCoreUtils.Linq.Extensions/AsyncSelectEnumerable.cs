using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace NCoreUtils.Linq
{
    sealed class AsyncSelectEnumerable<TSource, TResult> : IAsyncEnumerable<TResult>
    {
        public IAsyncEnumerable<TSource> Source { get; }

        public Func<TSource, CancellationToken, Task<TResult>> Selector { get; }

        public AsyncSelectEnumerable(IAsyncEnumerable<TSource> source, Func<TSource, CancellationToken, Task<TResult>> selector)
        {
            Source = source;
            Selector = selector;
        }

        public IAsyncEnumerator<TResult> GetEnumerator() => new AsyncSelectEnumerator<TSource, TResult>(Source.GetEnumerator(), Selector);
    }
}