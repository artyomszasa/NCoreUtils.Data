using System;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

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

        internal void ClearTransaction()
        {
            _tx = null;
        }
    }
}