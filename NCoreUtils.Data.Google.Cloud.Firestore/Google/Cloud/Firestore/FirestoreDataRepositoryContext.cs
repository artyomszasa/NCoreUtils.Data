using System;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Google.Cloud.Firestore;
using Microsoft.Extensions.Logging;

namespace NCoreUtils.Data.Google.Cloud.Firestore;

public class FirestoreDataRepositoryContext : IDataRepositoryContext, IFirestoreDbAccessor
{
    private int _isDisposed;

    internal FirestoreDataTransaction? _tx;

    IDataTransaction? IDataRepositoryContext.CurrentTransaction => CurrentTransaction;

    protected ILoggerFactory LoggerFactory { get; }

    public FirestoreDb Db { get; }

    public FirestoreDataTransaction? CurrentTransaction { get; }

    public FirestoreDataRepositoryContext(ILoggerFactory loggerFactory, FirestoreDb db)
    {
        LoggerFactory = loggerFactory;
        Db = db;
    }

    ValueTask<IDataTransaction> IDataRepositoryContext.BeginTransactionAsync(IsolationLevel isolationLevel, CancellationToken cancellationToken)
        => new(BeginTransaction(isolationLevel));

    Task IFirestoreDbAccessor.ExecuteAsync(Func<FirestoreDb, Task> action)
    {
        var tx = Interlocked.CompareExchange(ref _tx, null, null);
        if (tx is null)
        {
            return action(Db);
        }
        return tx.ExecuteAsync(ftx => action(ftx.Database));
    }

    Task<T> IFirestoreDbAccessor.ExecuteAsync<T>(Func<FirestoreDb, Task<T>> action)
    {
        var tx = Interlocked.CompareExchange(ref _tx, null, null);
        if (tx is null)
        {
            return action(Db);
        }
        return tx.ExecuteAsync(ftx => action(ftx.Database));
    }

    internal void Unlink(FirestoreDataTransaction tx)
        => Interlocked.CompareExchange(ref _tx, default, tx);

    protected virtual void Dispose(bool disposing)
    {
        if (0 == Interlocked.CompareExchange(ref _isDisposed, 1, 0))
        {
            _tx?.Dispose();
        }
    }

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

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}