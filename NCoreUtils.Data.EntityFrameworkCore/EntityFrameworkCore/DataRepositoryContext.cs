using System.Data;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Microsoft.EntityFrameworkCore;

namespace NCoreUtils.Data.EntityFrameworkCore;

public abstract class DataRepositoryContext : IDataRepositoryContext
{
    int _isDisposed;

    IDataTransaction? IDataRepositoryContext.CurrentTransaction
    {
        [ExcludeFromCodeCoverage]
        get => CurrentTransaction;
    }

    public DataTransaction? CurrentTransaction { get; protected set; }

    public abstract DbContext DbContext { get; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [DebuggerStepThrough]
    internal void ReleaseTransaction() => CurrentTransaction = null;

    public abstract ValueTask<IDataTransaction> BeginTransactionAsync(IsolationLevel isolationLevel, CancellationToken cancellationToken = default);

    #region disposable

    protected virtual void Dispose(bool disposing)
    {
        if (disposing && 0 == Interlocked.CompareExchange(ref _isDisposed, 1, 0))
        {
            CurrentTransaction?.Dispose();
        }
    }

    protected virtual ValueTask DisposeAsyncCore()
    {
        if (0 == Interlocked.CompareExchange(ref _isDisposed, 1, 0))
        {
            if (CurrentTransaction is DataTransaction tx)
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
        await DisposeAsyncCore();
        Dispose(disposing: false);
        GC.SuppressFinalize(this);
    }

    #endregion
}

public sealed class DataRepositoryContext<TDbContext>(TDbContext dbContext)
    : DataRepositoryContext
    where TDbContext : DbContext
{
    readonly TDbContext _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));

    public override DbContext DbContext => _dbContext;

    public override async ValueTask<IDataTransaction> BeginTransactionAsync(IsolationLevel isolationLevel, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (null != CurrentTransaction)
        {
            throw new InvalidOperationException("Transaction has already been started in the actual context.");
        }
        var tx = await _dbContext.Database.BeginTransactionAsync(isolationLevel, cancellationToken);
        CurrentTransaction = new DataTransaction(this, tx);
        return CurrentTransaction;
    }
}