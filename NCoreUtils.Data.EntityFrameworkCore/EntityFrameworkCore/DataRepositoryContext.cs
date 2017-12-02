using System;
using System.Data;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace NCoreUtils.Data.EntityFrameworkCore
{
    public abstract class DataRepositoryContext : IDataRepositoryContext
    {
        int _isDisposed;

        IDataTransaction IDataRepositoryContext.CurrentTransaction => CurrentTransaction;

        protected bool IsDisposed
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            [DebuggerStepThrough]
            get => 0 != _isDisposed;
        }

        public DataTransaction CurrentTransaction { get; protected set; }

        public abstract DbContext DbContext { get; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [DebuggerStepThrough]
        internal void ReleaseTransaction() => CurrentTransaction = null;

        protected abstract void DisposeOnce(bool disposing);

        protected virtual void Dispose(bool disposing)
        {
            if (0 == Interlocked.CompareExchange(ref _isDisposed, 1, 0))
            {
                DisposeOnce(disposing);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [DebuggerStepThrough]
        protected void ThrowIfDisposed()
        {
            if (IsDisposed)
            {
                throw new ObjectDisposedException(this.GetType().Name);
            }
        }

        public abstract Task<IDataTransaction> BeginTransactionAsync(IsolationLevel isolationLevel, CancellationToken cancellationToken = default(CancellationToken));

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }

    sealed class DataRepositoryContext<TDbContext> : DataRepositoryContext
        where TDbContext : DbContext
    {
        readonly TDbContext _dbContext;

        public override DbContext DbContext => _dbContext;

        public DataRepositoryContext(TDbContext dbContext)
            => _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));

        protected override void DisposeOnce(bool disposing) => CurrentTransaction?.Dispose();

        public override async Task<IDataTransaction> BeginTransactionAsync(IsolationLevel isolationLevel, CancellationToken cancellationToken = default(CancellationToken))
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
}