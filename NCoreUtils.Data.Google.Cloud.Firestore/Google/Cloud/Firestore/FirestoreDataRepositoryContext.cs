using System.Data;
using System.Diagnostics.CodeAnalysis;
using Google.Cloud.Firestore;
using Microsoft.Extensions.Logging;

namespace NCoreUtils.Data.Google.Cloud.Firestore;

[SuppressMessage("Design", "CA1033:Interface methods should be callable by child types", Justification = "Should not be accessed from derived classes")]
public class FirestoreDataRepositoryContext : IDataRepositoryContext, IFirestoreDbAccessor
{
    private int _isDisposed;

    internal FirestoreDataTransaction? _tx;

    IDataTransaction? IDataRepositoryContext.CurrentTransaction => CurrentTransaction;

    protected ILoggerFactory LoggerFactory { get; }

    public FirestoreDb Db { get; }

    public FirestoreDataTransaction? CurrentTransaction => _tx;

    public FirestoreDataRepositoryContext(ILoggerFactory loggerFactory, FirestoreDb db)
    {
        LoggerFactory = loggerFactory;
        Db = db;
    }

    ValueTask<IDataTransaction> IDataRepositoryContext.BeginTransactionAsync(IsolationLevel isolationLevel, CancellationToken cancellationToken)
        => new(BeginTransaction(isolationLevel));

    Task IFirestoreDbAccessor.ExecuteAsync(
        Func<FirestoreDb, Transaction?, CancellationToken, Task> action,
        CancellationToken cancellationToken)
    {
        var tx = Interlocked.CompareExchange(ref _tx, null, null);
        if (tx is null)
        {
            return action(Db, default, cancellationToken);
        }
        return tx.ExecuteAsync((ftx, cancellationToken) => action(ftx.Database, ftx, cancellationToken), cancellationToken);
    }

    Task<T> IFirestoreDbAccessor.ExecuteAsync<T>(
        Func<FirestoreDb, Transaction?, CancellationToken, Task<T>> action,
        CancellationToken cancellationToken)
    {
        var tx = Interlocked.CompareExchange(ref _tx, null, null);
        if (tx is null)
        {
            return action(Db, default, cancellationToken);
        }
        return tx.ExecuteAsync((ftx, cancellationToken) => action(ftx.Database, ftx, cancellationToken), cancellationToken);
    }

    internal void Unlink(FirestoreDataTransaction tx)
        => Interlocked.CompareExchange(ref _tx, default, tx);

    [SuppressMessage("Microsoft.Performance", "CA1801:ReviewUnusedParameters", MessageId = "isolationLevel")]
    [SuppressMessage("Style", "IDE0060:Remove unused parameter", MessageId = "isolationLevel")]
    public FirestoreDataTransaction BeginTransaction(IsolationLevel isolationLevel)
    {
        var tx0 = Interlocked.CompareExchange(ref _tx, null, null);
        if (null != tx0)
        {
            throw new InvalidOperationException($"Transaction {tx0.Guid} is already active on the current repository context.");
        }
        var tx = new FirestoreDataTransaction(
            LoggerFactory.CreateLogger<FirestoreDataTransaction>(),
            this,
            Db);
        if (null != Interlocked.CompareExchange(ref _tx, tx, null))
        {
            tx.Dispose();
            throw new InvalidOperationException($"Failed to start a transaction due to concurrency issues.");
        }
        return tx;
    }

    #region disposable

    protected virtual void Dispose(bool disposing)
    {
        if (0 == Interlocked.CompareExchange(ref _isDisposed, 1, 0))
        {
            _tx?.Dispose();
        }
    }

    protected virtual ValueTask DisposeAsyncCore()
    {
        if (0 == Interlocked.CompareExchange(ref _isDisposed, 1, 0))
        {
            if (_tx is FirestoreDataTransaction tx)
            {
                return tx.DisposeAsync();
            }
        }
        return default;
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    public async ValueTask DisposeAsync()
    {
        await DisposeAsyncCore().ConfigureAwait(false);
        Dispose(disposing: false);
        GC.SuppressFinalize(this);
    }

    #endregion
}