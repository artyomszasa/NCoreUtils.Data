using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Google.Cloud.Firestore;
using Microsoft.Extensions.Logging;

namespace NCoreUtils.Data.Google.Cloud.Firestore
{
    public sealed partial class FirestoreDataTransaction : IDataTransaction
    {
        private readonly BufferBlock<Message> _queue = new();

        private readonly CancellationTokenSource _cancellation = new();

        private readonly Task _task;

        private readonly ILogger _logger;

        private readonly FirestoreDataRepositoryContext _context;

        private int _isCompleted;

        private int _isDisposed;

        public Guid Guid => Guid.NewGuid();

        public bool IsCompleted => 0 != Interlocked.CompareExchange(ref _isCompleted, 0, 0);

        public bool IsDisposed => 0 != Interlocked.CompareExchange(ref _isDisposed, 0, 0);

        public FirestoreDataTransaction(
            ILogger<FirestoreDataTransaction> logger,
            FirestoreDataRepositoryContext context,
            FirestoreDb db)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _task = db.RunTransactionAsync(Run, TransactionOptions.ForMaxAttempts(1), _cancellation.Token);
        }

        private void Rollback(bool ignoreDisposed)
        {
            if (!ignoreDisposed)
            {
                ThrowIfDisposed();
            }
            if (!_queue.Post(Message.Rollback))
            {
                throw new InvalidOperationException("Failed to post rollback message.");
            }
            Unlink();
            Interlocked.CompareExchange(ref _isCompleted, 1, 0);
        }

        private async Task Run(Transaction tx)
        {
            // force new task
            await Task.Yield();
            try
            {
                var shouldExit = false;
                while (!shouldExit)
                {
                    var message = await _queue.ReceiveAsync(tx.CancellationToken);
                    shouldExit = await message.RunAsync(tx);
                }
                _logger.LogDebug("Committing firestore transaction {Guid}.", Guid);
            }
            finally
            {
                Interlocked.CompareExchange(ref _isCompleted, 1, 0);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ThrowIfDisposed()
        {
            if (0 != Interlocked.CompareExchange(ref _isDisposed, 0, 0))
            {
                throw new ObjectDisposedException(nameof(FirestoreDataTransaction));
            }
        }

        private void Unlink()
        {
            _context.Unlink(this);
        }

        private bool WaitNoThrow(int milliseconds)
        {
            try
            {
                return _task.Wait(milliseconds);
            }
            catch (AggregateException aexn)
            {
                if (1 == aexn.InnerExceptions.Count)
                {
                    if (aexn.InnerExceptions[0] is AbortTransactionException)
                    {
                        // aborted normally
                        _logger.LogDebug("Firestore transaction {Guid} has been rolled back.", Guid);
                    }
                    else
                    {
                        _logger.LogError(aexn.InnerExceptions[0], "Unexpected exception while disposing firebase transaction {Guid}.", Guid);
                    }
                }
                else
                {
                    _logger.LogError(aexn.InnerExceptions[0], "Unexpected exception while disposing firebase transaction {Guid}.", Guid);
                }
                return true;
            }
        }

        public void Commit()
        {
            ThrowIfDisposed();
            if (!_queue.Post(Message.Commit))
            {
                throw new InvalidOperationException("Failed to post commit message.");
            }
            Unlink();
            // set early to avoid rollback in dispose
            Interlocked.CompareExchange(ref _isCompleted, 1, 0);
        }

        public void Dispose()
        {
            if (0 == Interlocked.CompareExchange(ref _isDisposed, 1, 0))
            {
                if (!IsCompleted)
                {
                    // still in transaction
                    Rollback(true);
                    if (!WaitNoThrow(20))
                    {
                        // executor task has not finished
                        _cancellation.Cancel();
                        if (!WaitNoThrow(50))
                        {
                            _logger.LogError("Firebase transaction have not finished.");
                        }
                    }
                }
                _queue.Complete();
                _cancellation.Dispose();
                try { _task.Dispose(); } catch { }
            }
        }

        public Task<T> ExecuteAsync<T>(Func<Transaction, Task<T>> action)
        {
            var message = Message.Action(action);
            if (!_queue.Post(message))
            {
                throw new InvalidOperationException("Failed to post action message.");
            }
            return message.Completion.Task;
        }

        public Task ExecuteAsync(Func<Transaction, Task> action)
        {
            var message = Message.Action<bool>(async (tx) =>
            {
                await action(tx);
                return default;
            });
            if (!_queue.Post(message))
            {
                throw new InvalidOperationException("Failed to post action message.");
            }
            return message.Completion.Task;
        }

        public void Rollback()
            => Rollback(false);
    }
}