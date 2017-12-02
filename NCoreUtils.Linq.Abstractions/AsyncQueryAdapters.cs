using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace NCoreUtils.Linq
{
    public static class AsyncQueryAdapters
    {
        sealed class ByReferenceEqualityComparer<T> : IEqualityComparer<T>
        {
            public bool Equals(T x, T y) => ReferenceEquals(x, y);

            public int GetHashCode(T obj) => RuntimeHelpers.GetHashCode(obj);
        }

        static readonly ByReferenceEqualityComparer<IAsyncQueryAdapter> _eq = new ByReferenceEqualityComparer<IAsyncQueryAdapter>();

        static readonly List<IAsyncQueryAdapter> _adapters = new List<IAsyncQueryAdapter>();
        static int _sync;

        static void Synced(Action action)
        {
            while (0 != Interlocked.CompareExchange(ref _sync, 1, 0)) { }
            try
            {
                action();
            }
            finally
            {
                _sync = 0;
            }
        }

        public static void Add(IAsyncQueryAdapter adapter)
        {
            Synced(() =>
            {
                if (!_adapters.Contains(adapter, _eq))
                {
                    _adapters.Add(adapter);
                }
            });
        }

        public static async Task<IAsyncQueryProvider> AdaptAsync(IQueryProvider provider, CancellationToken cancellationToken)
        {
            while (0 != Interlocked.CompareExchange(ref _sync, 1, 0)) { }
            try
            {
                if (0 == _adapters.Count)
                {
                    return null;
                }
                var i = 0;
                Func<Task<IAsyncQueryProvider>> next = null;
                next = () =>
                {
                    ++i;
                    if (i >= _adapters.Count)
                    {
                        return Task.FromResult<IAsyncQueryProvider>(null);
                    }
                    return _adapters[i].GetAdapterAsync(next, provider, cancellationToken);
                };
                return await _adapters[0].GetAdapterAsync(next, provider, cancellationToken);
            }
            finally
            {
                _sync = 0;
            }
        }
    }
}