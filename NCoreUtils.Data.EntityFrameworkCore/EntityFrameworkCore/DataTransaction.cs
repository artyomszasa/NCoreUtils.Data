using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.EntityFrameworkCore.Storage;

namespace NCoreUtils.Data.EntityFrameworkCore
{
    public sealed class DataTransaction : IDataTransaction
    {
        readonly DataRepositoryContext _context;

        readonly IDbContextTransaction _dbTransaction;

        int _isDisposed;

        int _isFinished;

        public event EventHandler OnCommit;

        public event EventHandler OnRollback;

        public Guid Guid { get; } = Guid.NewGuid();

        public DataTransaction(DataRepositoryContext context, IDbContextTransaction dbTransaction)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _dbTransaction = dbTransaction ?? throw new ArgumentNullException(nameof(dbTransaction));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [DebuggerStepThrough]
        void ThrowAlreadyFinished()
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

        void CommitImplementation()
        {
            _context.ReleaseTransaction();
            OnCommit?.Invoke(this, EventArgs.Empty);
            _dbTransaction.Commit();
        }

        void RollbackImplemtation()
        {
            _context.ReleaseTransaction();
            OnRollback?.Invoke(this, EventArgs.Empty);
            _dbTransaction.Rollback();
        }

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

        public void Dispose()
        {
            if (0 == Interlocked.CompareExchange(ref _isDisposed, 1, 0))
            {
                if (0 == Interlocked.CompareExchange(ref _isFinished, 1, 0))
                {
                    try
                    {
                        RollbackImplemtation();
                    }
                    catch { } // TODO: valami loggolás?
                }
                _dbTransaction.Dispose();
            }
        }

        public void Rollback()
        {
            if (0 == Interlocked.CompareExchange(ref _isFinished, 1, 0))
            {
                RollbackImplemtation();
            }
            else
            {
                ThrowAlreadyFinished();
            }
        }
    }
}