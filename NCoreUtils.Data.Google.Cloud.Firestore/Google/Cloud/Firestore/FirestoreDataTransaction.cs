using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using Google.Cloud.Firestore;
using Microsoft.Extensions.Logging;
using NCoreUtils.Data.Google.Cloud.Firestore.Internal;

using RpcException = Grpc.Core.RpcException;

namespace NCoreUtils.Data.Google.Cloud.Firestore;

public sealed partial class FirestoreDataTransaction : IDataTransaction
{
#if NETSTANDARD2_1
    private static async Task<bool> WaitForAsync(Task task, TimeSpan timeout)
    {
        await Task.WhenAny(task, Task.Delay(timeout));
        if (task.IsCompletedSuccessfully)
        {
            return true;
        }
        if (task.IsFaulted || task.IsCanceled)
        {
            await task;
            return true; // dummy
        }
        return false;
    }
#else
    private static async Task<bool> WaitForAsync(Task task, TimeSpan timeout)
    {
        await task.WaitAsync(timeout);
        return task.IsCompletedSuccessfully;
    }
#endif

    private readonly Channel<Message> _queue = Channel.CreateUnbounded<Message>(new UnboundedChannelOptions
    {
        AllowSynchronousContinuations = false,
        SingleReader = true,
        SingleWriter = false
    });

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

    private bool DoPostMessage(Message message)
        // NOTE: for unbounded channels it only returns false when channel is completed...
        => _queue.Writer.TryWrite(message);

    private ValueTask<Message> DoReceiveMessageAsync(CancellationToken cancellationToken)
        => _queue.Reader.ReadAsync(cancellationToken);

    private void Rollback(bool ignoreDisposed)
    {
        if (!ignoreDisposed)
        {
            ThrowIfDisposed();
        }
        if (!DoPostMessage(Message.Rollback))
        {
            throw new InvalidOperationException("Failed to post rollback message.");
        }
        Unlink();
        Interlocked.CompareExchange(ref _isCompleted, 1, 0);
    }

    private ValueTask RollbackAsync(bool ignoreDisposed, CancellationToken cancellationToken)
    {
        if (!ignoreDisposed)
        {
            ThrowIfDisposed();
        }
        var message = Message.RollbackAsync();
        if (!DoPostMessage(message))
        {
            throw new InvalidOperationException("Failed to post rollback message.");
        }
        Unlink();
        Interlocked.CompareExchange(ref _isCompleted, 1, 0);
        return new(message.Completion.Task);
    }

    private async Task Run(Transaction tx)
    {
        // force new task
        await Task.Yield();
        if (IsCompleted)
        {
            // SHOULD NEVER HAPPEN DUE TO TransactionOptions.MaxAttempts == 1
            _logger.LogTransactionUnexpectedRetry(Guid);
        }
        try
        {
            var stopwatch = new Stopwatch();
            var shouldExit = false;
            while (!shouldExit)
            {
                _logger.LogTransactionWaitForMessages(Guid);
                var message = await DoReceiveMessageAsync(tx.CancellationToken);
                if (_logger.IsEnabled(LogLevel.Trace))
                {
                    _logger.LogTransactionExecutingMessage(Guid, message.ToString());
                }
                stopwatch.Restart();
                shouldExit = await message.RunAsync(tx);
                stopwatch.Stop();
                if (_logger.IsEnabled(LogLevel.Trace))
                {
                    _logger.LogTransactionExecutedMessage(Guid, message.ToString(), stopwatch.ElapsedMilliseconds, shouldExit);
                }
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

    private void LogTxException(Exception exn)
    {
        if (exn is OperationCanceledException or RpcException { StatusCode: Grpc.Core.StatusCode.Cancelled })
        {
            // Cancelled --> finished without perfomring anything.
            return;
        }
        else if (exn is AbortTransactionException)
        {
            // aborted normally
            _logger.LogTransactionRollback(Guid);
        }
        else if (exn is AggregateException aexn)
        {
            foreach (var innerException in aexn.InnerExceptions)
            {
                LogTxException(exn);
            }
        }
        else
        {
            _logger.LogTransactionUnexpectedExceptionOnDispose(exn, Guid);
        }
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
        catch (Exception exn)
        {
            LogTxException(exn);
            return true;
        }
    }

    private async ValueTask<bool> WaitNoThrowAsync(TimeSpan timeout)
    {
        try
        {
            if (_task.IsCanceled)
            {
                return true;
            }
            return await WaitForAsync(_task, timeout);
        }
        catch (Exception exn)
        {
            LogTxException(exn);
            return true;
        }
    }

    [Obsolete("Use async version when possible")]
    public void Commit()
    {
        ThrowIfDisposed();
        if (!DoPostMessage(Message.Commit))
        {
            throw new InvalidOperationException("Failed to post commit message.");
        }
        Unlink();
        // set early to avoid rollback in dispose
        Interlocked.CompareExchange(ref _isCompleted, 1, 0);
    }

    public ValueTask CommitAsync(CancellationToken cancellationToken)
    {
        ThrowIfDisposed();
        cancellationToken.ThrowIfCancellationRequested();
        var message = Message.CommitAsync();
        if (!DoPostMessage(message))
        {
            throw new InvalidOperationException("Failed to post commit message.");
        }
        Unlink();
        // set early to avoid rollback in dispose
        Interlocked.CompareExchange(ref _isCompleted, 1, 0);
        return new(message.Completion.Task);
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
                if (!WaitNoThrow(milliseconds: 200))
                {
                    _logger.LogTransactionTaskNotFinished(Guid);
                }
            }
            _queue.Writer.Complete();
            _cancellation.Dispose();
            try { _task.Dispose(); } catch { }
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (0 == Interlocked.CompareExchange(ref _isDisposed, 1, 0))
        {
            if (!IsCompleted)
            {
                // still in transaction
                _logger.LogTransactionRollbackOnDispose(Guid);
                await RollbackAsync(true, CancellationToken.None);
            }
            if (!await WaitNoThrowAsync(timeout: TimeSpan.FromMilliseconds(20)))
            {
                _cancellation.Cancel();
                if (!await WaitNoThrowAsync(timeout: TimeSpan.FromMilliseconds(200)))
                {
                    _logger.LogTransactionTaskNotFinished(Guid);
                }
            }
            _queue.Writer.Complete();
            _cancellation.Dispose();
            try { _task.Dispose(); } catch { }
        }
    }

    public Task<T> ExecuteAsync<T>(Func<Transaction, Task<T>> action)
    {
        var message = Message.Action(action);
        if (!DoPostMessage(message))
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
        if (!DoPostMessage(message))
        {
            throw new InvalidOperationException("Failed to post action message.");
        }
        return message.Completion.Task;
    }

    [Obsolete("Use async version when possible")]
    public void Rollback()
        => Rollback(false);

    public ValueTask RollbackAsync(CancellationToken cancellationToken)
        => RollbackAsync(false, cancellationToken);
}