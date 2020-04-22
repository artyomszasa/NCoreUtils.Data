using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Google.Cloud.Firestore;
using Microsoft.Extensions.Logging;
using NCoreUtils.Data.Google.FireStore.Builders;
using NCoreUtils.Data.Google.FireStore.Queries;

namespace NCoreUtils.Data.Google.FireStore
{
    public class DataRepositoryContext : IDataRepositoryContext
    {


        readonly ILoggerFactory _loggerFactory;

        readonly FireStoreDbFactory _dbFactory;

        int _isDiposed;

        DataTransaction _currentTransaction;

        IDataTransaction IDataRepositoryContext.CurrentTransaction => CurrentTransaction;

        public QueryProvider Provider { get; }

        public FirestoreDb Database { get; }

        public DataTransaction CurrentTransaction => _currentTransaction;

        public Model Model => _dbFactory.Model;

        public DataRepositoryContext(ILoggerFactory loggerFactory, FireStoreDbFactory dbFactory)
        {
            _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
            _dbFactory = dbFactory ?? throw new ArgumentNullException(nameof(dbFactory));
            Database = dbFactory.Rent();
            Provider = new QueryProvider(this, Database);
        }

        ValueTask<IDataTransaction> IDataRepositoryContext.BeginTransactionAsync(IsolationLevel isolationLevel, CancellationToken cancellationToken)
            => new ValueTask<IDataTransaction>(BeginTransaction());

        internal bool Unlink(DataTransaction tx)
        {
            return ReferenceEquals(tx, Interlocked.CompareExchange(ref _currentTransaction, null, tx));
        }

        protected virtual void Dispose(bool disposing)
        {
            if (0 == Interlocked.CompareExchange(ref _isDiposed, 1, 0))
            {
                _currentTransaction?.Dispose();
                _dbFactory.Return(Database);
            }
        }

        public DataTransaction BeginTransaction()
        {
            var tx = new DataTransaction(this, _loggerFactory.CreateLogger<DataTransaction>());
            if (!(Interlocked.CompareExchange(ref _currentTransaction, tx, null) is null))
            {
                tx.Dispose();
                throw new InvalidOperationException("Already in transaction.");
            }
            return tx;
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            Dispose(true);
        }
    }
}