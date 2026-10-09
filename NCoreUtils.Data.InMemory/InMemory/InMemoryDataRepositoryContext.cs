using System.Data;
using System.Diagnostics.CodeAnalysis;

namespace NCoreUtils.Data.InMemory
{
    [ExcludeFromCodeCoverage]
    public sealed class InMemoryDataRepositoryContext : IDataRepositoryContext
    {
        private NoopTransaction? _tx;

        public IDataTransaction? CurrentTransaction => _tx;

        public ValueTask<IDataTransaction> BeginTransactionAsync(IsolationLevel isolationLevel, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (null != CurrentTransaction)
            {
                throw new InvalidOperationException("Transaction has already been started in the actual context.");
            }
            _tx = new NoopTransaction(this);
            return new ValueTask<IDataTransaction>(_tx);
        }

        public void Dispose() => _tx?.Dispose();

        public ValueTask DisposeAsync()
        {
            if (_tx is NoopTransaction tx)
            {
                return tx.DisposeAsync();
            }
            return default;
        }

        internal void ClearTransaction()
        {
            _tx = null;
        }
    }
}