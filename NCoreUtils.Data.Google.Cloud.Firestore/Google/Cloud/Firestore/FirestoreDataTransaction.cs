using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Google.Cloud.Firestore;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using NCoreUtils.Data.Google.Cloud.Firestore.Internal;

namespace NCoreUtils.Data.Google.Cloud.Firestore;

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
            var stopwatch = new Stopwatch();
            var shouldExit = false;
            while (!shouldExit)
            {
                _logger.LogTransactionWaitForMessages(Guid);
                var message = await _queue.ReceiveAsync(tx.CancellationToken);
                _logger.LogTransactionExecutingMessage(Guid, message.ToString());
                stopwatch.Restart();
                shouldExit = await message.RunAsync(tx);
                stopwatch.Stop();
                _logger.LogTransactionExecutedMessage(Guid, message.ToString(), stopwatch.ElapsedMilliseconds, shouldExit);
            }
            _logger.LogTransactionCommitting(Guid);
        }
        finally
        {
            Interlocked.CompareExchange(ref _isCompleted, 1, 0);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ThrowIfDisposed()
    {
#if NET8_0_OR_GREATER
        ObjectDisposedException.ThrowIf(0 != Interlocked.CompareExchange(ref _isDisposed, 0, 0), this);
#else
        if (0 != Interlocked.CompareExchange(ref _isDisposed, 0, 0))
        {
            throw new ObjectDisposedException(nameof(FirestoreDataTransaction));
        }
#endif
    }

    private void Unlink()
    {
        _context.Unlink(this);
    }

    private bool WaitNoThrow(int milliseconds)
    {
        try
        {
            if (_task.IsCanceled)
            {
                return true;
            }
            return _task.Wait(milliseconds);
        }
        catch (OperationCanceledException)
        {
            return true;
        }
        catch (AggregateException aexn)
        {
            if (1 == aexn.InnerExceptions.Count)
            {
                switch (aexn.InnerExceptions[0])
                {
                    case AbortTransactionException:
                        // aborted normally
                        _logger.LogTransactionRollback(Guid);
                        break;
                    case RpcException rpcException when rpcException.StatusCode == StatusCode.Cancelled:
                    case OperationCanceledException:
                        // Cancelled --> finished without perfomring anything.
                        break;
                    case var innerException:
                        _logger.LogTransactionUnexpectedExceptionOnDispose(innerException, Guid);
                        break;
                }
            }
            else
            {
                _logger.LogTransactionUnexpectedExceptionOnDispose(aexn, Guid);
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
                _logger.LogTransactionRollbackOnDispose(Guid);
                Rollback(true);
            }
            if (!WaitNoThrow(milliseconds: 20))
            {
                // executor task has not finished
                _cancellation.Cancel();
                if (!WaitNoThrow(milliseconds: 50))
                {
                    _logger.LogTransactionTaskNotFinished(Guid);
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