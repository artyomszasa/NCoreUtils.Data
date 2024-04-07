using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace NCoreUtils.Data.Rest;

public sealed class RestDataRepositoryContext : IDataRepositoryContext
{
    internal IDataTransaction? _tx;

    public IDataTransaction? CurrentTransaction => _tx;

    public ValueTask<IDataTransaction> BeginTransactionAsync(IsolationLevel isolationLevel, CancellationToken cancellationToken = default)
    {
        if (null == Interlocked.CompareExchange(ref _tx, new RestDataTransaction(this), null))
        {
            return new ValueTask<IDataTransaction>(_tx);
        }
        throw new InvalidOperationException($"Multiple transactions are not allowed (current ttransaction = {_tx.Guid}).");
    }

    public void Dispose() { /* noop */ }
}