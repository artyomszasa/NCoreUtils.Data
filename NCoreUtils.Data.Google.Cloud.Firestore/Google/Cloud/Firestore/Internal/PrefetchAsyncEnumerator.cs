using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace NCoreUtils.Data.Google.Cloud.Firestore.Internal
{
    public sealed class PrefetchAsyncEnumerator<T> : IAsyncDisposable
    {
        private readonly IAsyncEnumerator<T> _source;

        private bool _consumed;

        private Maybe<T> _current;

        public PrefetchAsyncEnumerator(IAsyncEnumerable<T> source, CancellationToken cancellationToken)
        {
            _source = source.GetAsyncEnumerator(cancellationToken);
        }

        public async ValueTask<Maybe<T>> GetCurrentAsync()
        {
            if (_consumed)
            {
                return default;
            }
            if (_current.TryGetValue(out var current))
            {
                return current.Just();
            }
            if (await _source.MoveNextAsync())
            {
                _current = _source.Current.Just();
                return _current;
            }
            _consumed = true;
            _current = default;
            return default;
        }

        public void Consume()
        {
            _current = default;
        }

        public ValueTask DisposeAsync()
            => _source.DisposeAsync();
    }
}