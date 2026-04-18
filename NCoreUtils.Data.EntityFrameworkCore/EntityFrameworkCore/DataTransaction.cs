using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Microsoft.EntityFrameworkCore.Storage;

namespace NCoreUtils.Data.EntityFrameworkCore;

public sealed class DataTransaction(DataRepositoryContext context, IDbContextTransaction dbTransaction)
    : IDataTransaction
{
    readonly DataRepositoryContext _context = context ?? throw new ArgumentNullException(nameof(context));

    readonly IDbContextTransaction _dbTransaction = dbTransaction ?? throw new ArgumentNullException(nameof(dbTransaction));

    int _isDisposed;

    int _isFinished;

    public event EventHandler? OnCommit;

    public event EventHandler? OnRollback;

    [ExcludeFromCodeCoverage]
    public Guid Guid { get; } = Guid.NewGuid();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [DebuggerStepThrough]
    [DoesNotReturn]
    private static void ThrowAlreadyFinished()
    {
        throw new InvalidOperationException("Transaction has already been finished.");
    }

    // [MethodImpl(MethodImplOptions.AggressiveInlining)]
    // [DebuggerStepThrough]
    // void ThrowIfDisposed()
    // {
    //     if (0 != _isDisposed)
    //     {
    //         throw new ObjectDisposedException(nameof(DataTransaction));
    //     }
    // }

    private ValueTask DoCommitAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _context.ReleaseTransaction();
        OnCommit?.Invoke(this, EventArgs.Empty);
        return new(_dbTransaction.CommitAsync(cancellationToken));
    }

    private ValueTask DoRollbackAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _context.ReleaseTransaction();
        OnRollback?.Invoke(this, EventArgs.Empty);
        return new(_dbTransaction.RollbackAsync(cancellationToken));
    }

    [Obsolete("Use async version when possible")]
    private void CommitImplementation()
    {
        _context.ReleaseTransaction();
        OnCommit?.Invoke(this, EventArgs.Empty);
        _dbTransaction.Commit();
    }

    [Obsolete("Use async version when possible")]
    private void RollbackImplementation()
    {
        _context.ReleaseTransaction();
        OnRollback?.Invoke(this, EventArgs.Empty);
        _dbTransaction.Rollback();
    }

    [Obsolete("Use async version when possible")]
    public void Commit()
    {
        if (0 == Interlocked.CompareExchange(ref _isFinished, 1, 0))
        {
            CommitImplementation();
        }
        else
        {
            ThrowAlreadyFinished();
        }
    }

    public ValueTask CommitAsync(CancellationToken cancellationToken)
    {
        if (0 == Interlocked.CompareExchange(ref _isFinished, 1, 0))
        {
            return DoCommitAsync(cancellationToken);
        }
        else
        {
            ThrowAlreadyFinished();
            return default; // NOTE: dummy
        }
    }

    public void Dispose()
    {
        if (0 == Interlocked.CompareExchange(ref _isDisposed, 1, 0))
        {
            if (0 == Interlocked.CompareExchange(ref _isFinished, 1, 0))
            {
                try
                {
#pragma warning disable CS0618 // Type or member is obsolete
                    RollbackImplementation();
#pragma warning restore CS0618 // Type or member is obsolete
                }
                catch { } // TODO: valami loggolás?
            }
            _dbTransaction.Dispose();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (0 == Interlocked.CompareExchange(ref _isDisposed, 1, 0))
        {
            if (0 == Interlocked.CompareExchange(ref _isFinished, 1, 0))
            {
                try
                {
                    await DoRollbackAsync(CancellationToken.None);
                }
                catch { } // TODO: valami loggolás?
            }
            _dbTransaction.Dispose();
        }
    }

    [Obsolete("Use async version when possible")]
    public void Rollback()
    {
        if (0 == Interlocked.CompareExchange(ref _isFinished, 1, 0))
        {
            RollbackImplementation();
        }
        else
        {
            ThrowAlreadyFinished();
        }
    }

    public ValueTask RollbackAsync(CancellationToken cancellationToken)
    {
        if (0 == Interlocked.CompareExchange(ref _isFinished, 1, 0))
        {
            return DoRollbackAsync(cancellationToken);
        }
        else
        {
            ThrowAlreadyFinished();
            return default; // NOTE: dummy
        }
    }
}