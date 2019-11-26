using System;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Google.Cloud.Firestore;
using Microsoft.Extensions.Logging;

namespace NCoreUtils.Data.Google.FireStore
{
    public sealed class DataTransaction : IDataTransaction
    {
        abstract class Message
        {
            public static CommitMessage Commit { get; } = new CommitMessage();

            public static RollbackMessage Rollback { get; } = new RollbackMessage();

            public static ActionMessage<T> Action<T>(Func<Transaction, Task<T>> action) => new ActionMessage<T>(action);

            protected Message() { }

            public abstract ValueTask<bool> RunAsync(Transaction tx);
        }

        class CommitMessage : Message
        {
            public override ValueTask<bool> RunAsync(Transaction tx)
            {
                return new ValueTask<bool>(true);
            }
        }

        class RollbackMessage : Message
        {
            public override ValueTask<bool> RunAsync(Transaction tx)
            {
                throw new AbortTransactionException();
            }
        }

        class ActionMessage<T> : Message
        {
            public TaskCompletionSource<T> Completion { get; } = new TaskCompletionSource<T>();

            public Func<Transaction, Task<T>> Action { get; }

            public ActionMessage(Func<Transaction, Task<T>> action)
                => Action = action ?? throw new ArgumentNullException(nameof(action));

            public override async ValueTask<bool> RunAsync(Transaction tx)
            {
                try
                {
                    var result = await Action(tx);
                    Completion.TrySetResult(result);
                    return false;
                }
                catch (Exception exn)
                {
                    if (exn is OperationCanceledException)
                    {
                        Completion.TrySetCanceled();
                    }
                    else
                    {
                        Completion.TrySetException(exn);
                    }
                    throw new AbortTransactionException();
                }
            }
        }

        readonly DataRepositoryContext _context;

        readonly ILogger _logger;

        readonly BufferBlock<Message> _queue = new BufferBlock<Message>();

        readonly CancellationTokenSource _cancellation = new CancellationTokenSource();

        readonly Task _task;

        int _isCompleted = 0;

        int _isDisposed;

        public Guid Guid { get; } = Guid.NewGuid();

        public DataTransaction(DataRepositoryContext context, ILogger<DataTransaction> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _task = context.Database.RunTransactionAsync(async (tx) =>
            {
                try
                {
                    var shouldExit = false;
                    while (!shouldExit)
                    {
                        var message = await _queue.ReceiveAsync(tx.CancellationToken);
                        shouldExit = await message.RunAsync(tx);
                    }
                    _logger.LogDebug("Committing firestore transaction {0}.", Guid);
                }
                finally
                {
                    Interlocked.CompareExchange(ref _isCompleted, 1, 0);
                }
            }, TransactionOptions.ForMaxAttempts(1), _cancellation.Token);
            _logger = logger;
        }

        void Rollback(bool ignoreDisposed)
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void ThrowIfDisposed()
        {
            if (0 != Interlocked.CompareExchange(ref _isDisposed, 0, 0))
            {
                throw new ObjectDisposedException(nameof(DataTransaction));
            }
        }

        void Unlink()
        {
            _context.Unlink(this);
        }

        bool WaitNoThrow(int milliseconds)
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
                        _logger.LogDebug("Firestore transaction {0} has been rolled back.", Guid);
                    }
                    else
                    {
                        _logger.LogError(aexn.InnerExceptions[0], "Unexpected exception while disposing firebase transaction {0}.", Guid);
                    }
                }
                else
                {
                    _logger.LogError(aexn.InnerExceptions[0], "Unexpected exception while disposing firebase transaction {0}.", Guid);
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
            Interlocked.CompareExchange(ref _isCompleted, 1, 0);
        }

        public void Dispose()
        {
            if (0 == Interlocked.CompareExchange(ref _isDisposed, 1, 0))
            {
                if (0 == Interlocked.CompareExchange(ref _isCompleted, 0, 0))
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
                    _queue.Complete();
                    _cancellation.Dispose();
                    _task.Dispose();
                }
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

        public void Rollback() => Rollback(false);
    }
}