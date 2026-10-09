using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using Google.Cloud.Firestore;
using Microsoft.Extensions.Logging;
using NCoreUtils.Data.Google.Cloud.Firestore.Internal;
using static NCoreUtils.Data.Google.Cloud.Firestore.Internal.FlowHelpers;

using RpcException = Grpc.Core.RpcException;

namespace NCoreUtils.Data.Google.Cloud.Firestore;

public sealed partial class FirestoreDataTransaction : IDataTransaction
{
    private static TransactionOptions SingleAttempt { get; } = TransactionOptions.ForMaxAttempts(1);

    private readonly Channel<Message> _queue = Channel.CreateUnbounded<Message>(new UnboundedChannelOptions
    {
        AllowSynchronousContinuations = false,
        SingleReader = true,
        SingleWriter = false
    });

    private SpinLock _initSync = new(enableThreadOwnerTracking: false);

    private SpinLock _continuationSync = new(enableThreadOwnerTracking: false);

    private readonly CancellationTokenSource _cancellation = new();


    private readonly ILogger _logger;

    private readonly FirestoreDataRepositoryContext _context;

    private readonly FirestoreDb _db;

    private Task? _task;

    private int _isStarted;

    private int _isCompleted;

    private int _isDisposed;

    private TaskCompletionSource? _onCompleted;

    private TaskCompletionSource? _onFailed;

    [SuppressMessage("Naming", "CA1720:Identifier contains type name", Justification = "Intended")]
    public Guid Guid { get; } = Guid.NewGuid();

    public bool IsCompleted => 0 != Interlocked.CompareExchange(ref _isCompleted, 0, 0);

    public bool IsDisposed => 0 != Interlocked.CompareExchange(ref _isDisposed, 0, 0);

    public FirestoreDataTransaction(
        ILogger<FirestoreDataTransaction> logger,
        FirestoreDataRepositoryContext context,
        FirestoreDb db)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _db = db ?? throw new ArgumentNullException(nameof(db));
    }

    private void HandleSuccess()
    {
        _logger.LogTransactionCommitted(Guid);
        if (Exchange(ref _onCompleted, null) is TaskCompletionSource onCompleted)
        {
            onCompleted.TrySetResult();
            // NOTE: _onFailed cannot have value if _onFailed was set.
        }
        else
        {
            _logger.LogTransactionNoOnCompletedOnSuccess(Guid);
            if (Exchange(ref _onFailed, null) is TaskCompletionSource onFailed)
            {
                // NOTE: when abort completion is expected (which should happen when AbortTransactionException
                // is thrown) mark the abort completion as cancelled..
                onFailed.TrySetCanceled();
            }
        }
    }

    private void HandleAbortion(AbortTransactionException exn)
    {
        _logger.LogTransactionRollback(Guid);
        if (Exchange(ref _onFailed, null) is TaskCompletionSource onFailed)
        {
            // NOTE: intended rollback should not emit exception, instead it triggers the failed completion.
            onFailed.TrySetResult();
            // NOTE: _onCompleted cannot have value if _onFailed was set.
        }
        else
        {
            _logger.LogTransactionNoOnFailedOnRollbak(Guid);
            if (Exchange(ref _onCompleted, null) is TaskCompletionSource onCompleted)
            {
                // NOTE: when normal completion is expected (which should never happen when AbortTransactionException
                // is thrown) propagate the exception..
                onCompleted.TrySetException(exn);
            }
        }
    }

    private void HandleCancellation(OperationCanceledException exn)
    {
        _logger.LogTransactionCancelled(Guid);
        if (Exchange(ref _onCompleted, null) is TaskCompletionSource onCompleted)
        {
            onCompleted.TrySetCanceled(exn.CancellationToken);
        }
        else if (Exchange(ref _onFailed, null) is TaskCompletionSource onFailed)
        {
            onFailed.TrySetCanceled(exn.CancellationToken);
        }
    }

    private void HandleError(Exception exn)
    {
        _logger.LogTransactionFailedDueToException(exn, Guid);
        if (Exchange(ref _onCompleted, null) is TaskCompletionSource onCompleted)
        {
            onCompleted.TrySetException(exn);
        }
        else if (Exchange(ref _onFailed, null) is TaskCompletionSource onFailed)
        {
            onFailed.TrySetException(exn);
        }
    }

    private async Task RunTransactionWithCompletionAsync()
    {
        try
        {
            await _db.RunTransactionAsync(Run, SingleAttempt, _cancellation.Token).ConfigureAwait(false);
            HandleSuccess();
        }
        catch (AbortTransactionException exn)
        {
            // transaction aborted explicitly
            HandleAbortion(exn);
        }
        catch (OperationCanceledException exn)
        {
            // trasnaction failed due to cancellation
            HandleCancellation(exn);
        }
        catch (Exception exn)
        {
            // Generic error occured during the transaction
            HandleError(exn);
            throw;
        }
        finally
        {
            Interlocked.CompareExchange(ref _isCompleted, 1, 0);
            _queue.Writer.TryComplete();
            // fail potentially pending messages (which should never happen when used as intended..)
            while (_queue.Reader.TryRead(out var message))
            {
                message.Abort();
            }
        }
    }

    private bool DoPostMessage(Message message)
    {
        if (IsCompleted)
        {
            _logger.LogTransactionAttemptToSendMessageAfterCompletion(Guid);
            return false;
        }
        var lockTaken = false;
        _initSync.Enter(ref lockTaken);
        try
        {
            _task ??= RunTransactionWithCompletionAsync();
        }
        finally
        {
            if (lockTaken)
            {
                _initSync.Exit();
            }
        }
        // NOTE: for unbounded channels it only returns false when channel is completed...
        return _queue.Writer.TryWrite(message);
    }

    private ValueTask<Message> DoReceiveMessageAsync(CancellationToken cancellationToken)
        => _queue.Reader.ReadAsync(cancellationToken);

    private void Rollback(bool ignoreDisposed)
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
        Task.Run(() => message.Completion.Task).GetAwaiter().GetResult();
    }

    private static async Task AwaitWithCancellation(TaskCompletionSource completion, CancellationToken cancellationToken)
    {
#pragma warning disable CA2007 // Consider calling ConfigureAwait on the awaited task
        await using var _ = cancellationToken.Register(
            boxed => ((TaskCompletionSource)boxed!).TrySetCanceled(),
            completion
        );
#pragma warning restore CA2007 // Consider calling ConfigureAwait on the awaited task
        await completion.Task.ConfigureAwait(false);
    }

    private static async Task<T> AwaitWithCancellation<T>(TaskCompletionSource<T> completion, CancellationToken cancellationToken)
    {
#pragma warning disable CA2007 // Consider calling ConfigureAwait on the awaited task
        await using var _ = cancellationToken.Register(
            boxed => ((TaskCompletionSource<T>)boxed!).TrySetCanceled(),
            completion
        );
#pragma warning restore CA2007 // Consider calling ConfigureAwait on the awaited task
        return await completion.Task.ConfigureAwait(false);
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
        return new(AwaitWithCancellation(message.Completion, cancellationToken));
    }

    private async Task<int> Run(Transaction tx)
    {
        if (0 != Interlocked.CompareExchange(ref _isStarted, 1, 0))
        {
            _logger.LogTransactionUnexpectedRetry(Guid);
            throw new FirestoreTransactionExpiredException();
        }
        // force thread pool
        await Task.Yield();
        if (IsCompleted)
        {
            // SHOULD NEVER HAPPEN DUE TO TransactionOptions.MaxAttempts == 1
            _logger.LogTransactionUnexpectedRetry(Guid);
            throw new FirestoreTransactionExpiredException();
        }
        try
        {
            var stopwatch = new Stopwatch();
            var shouldExit = false;
            while (!shouldExit)
            {
                _logger.LogTransactionWaitForMessages(Guid);
                var message = await DoReceiveMessageAsync(tx.CancellationToken).ConfigureAwait(false);
                _logger.LogTransactionExecutingMessage(Guid, message);
                stopwatch.Restart();
                shouldExit = await message.RunAsync(this, tx).ConfigureAwait(false);
                stopwatch.Stop();
                _logger.LogTransactionExecutedMessage(Guid, message, stopwatch.ElapsedMilliseconds, shouldExit);
            }
            _logger.LogTransactionCommitting(Guid);
        }
        finally
        {
            Interlocked.CompareExchange(ref _isCompleted, 1, 0);
        }
        return 0;
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
                LogTxException(innerException);
            }
        }
        else
        {
            _logger.LogTransactionUnexpectedExceptionOnDispose(exn, Guid);
        }
    }

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
    private async Task<bool> WaitForAsync(Task task, TimeSpan timeout)
    {
        try
        {
            await task.WaitAsync(timeout).ConfigureAwait(false);
            if (task.IsCompletedSuccessfully)
            {
                return true;
            }
            _logger.LogTransactionWaitFailed(Guid, task.Status, task.IsCanceled, task.IsCompleted, task.IsFaulted);
            return task.IsCompletedSuccessfully;
        }
        catch (TimeoutException)
        {
            _logger.LogTransactionWaitTimeout(Guid, task.Status, task.IsCanceled, task.IsCompleted, task.IsFaulted);
            return false;
        }
    }
#endif

    [MemberNotNullWhen(false, nameof(_task))]
    private bool WaitNoThrow(int milliseconds)
    {
        try
        {
            if (_task is null || _task.IsCanceled)
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
            if (_task is null || _task.IsCanceled)
            {
                return true;
            }
            return await WaitForAsync(_task, timeout).ConfigureAwait(false);
        }
        catch (Exception exn)
        {
            LogTxException(exn);
            return true;
        }
    }

    internal void SetOnCompleted(TaskCompletionSource completion)
    {
        var lockTaken = false;
        _continuationSync.Enter(ref lockTaken);
        try
        {
            if (_onFailed is TaskCompletionSource onFailed)
            {
                _logger.LogTransactionOnCompletedSetWhenOnFailedHasBeenSet(Guid);
                _onFailed = default;
                onFailed.TrySetCanceled();
            }
            if (_onCompleted is not null)
            {
                _logger.LogTransactionMultipleOnCompleted(Guid);
                _onCompleted.TrySetCanceled();
            }
            _onCompleted = completion;
        }
        finally
        {
            if (lockTaken)
            {
                _continuationSync.Exit();
            }
        }
    }

    internal void SetOnFailed(TaskCompletionSource completion)
    {
        var lockTaken = false;
        _continuationSync.Enter(ref lockTaken);
        try
        {
            if (_onCompleted is TaskCompletionSource onCompleted)
            {
                _logger.LogTransactionOnFailedSetWhenOnCompletedHasBeenSet(Guid);
                _onCompleted = default;
                onCompleted.TrySetCanceled();
            }
            if (_onFailed is not null)
            {
                _logger.LogTransactionMultipleOnFailed(Guid);
                _onFailed.TrySetCanceled();
            }
            _onFailed = completion;
        }
        finally
        {
            if (lockTaken)
            {
                _continuationSync.Exit();
            }
        }
    }

    [Obsolete("Use async version when possible")]
    public void Commit()
    {
        ThrowIfDisposed();
        var message = Message.CommitAsync();
        if (!DoPostMessage(message))
        {
            throw new InvalidOperationException("Failed to post commit message.");
        }
        Unlink();
        // set early to avoid rollback in dispose
        Interlocked.CompareExchange(ref _isCompleted, 1, 0);
        Task.Run(() => message.Completion.Task).GetAwaiter().GetResult();
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
        return new(AwaitWithCancellation(message.Completion, cancellationToken));
    }

    public Task<T> ExecuteAsync<T>(
        Func<Transaction, CancellationToken, Task<T>> action,
        CancellationToken cancellationToken)
    {
        var message = Message.Action(action, cancellationToken);
        if (!DoPostMessage(message))
        {
            throw new InvalidOperationException("Failed to post action message.");
        }
        return AwaitWithCancellation(message.Completion, cancellationToken);
    }

    public Task ExecuteAsync(
        Func<Transaction, CancellationToken, Task> action,
        CancellationToken cancellationToken)
    {
        var message = Message.Action<bool>(async (tx, cancellationToken) =>
        {
            await action(tx, cancellationToken).ConfigureAwait(false);
            return default;
        }, cancellationToken);
        if (!DoPostMessage(message))
        {
            throw new InvalidOperationException("Failed to post action message.");
        }
        return AwaitWithCancellation(message.Completion, cancellationToken);
    }

    [Obsolete("Use async version when possible")]
    public void Rollback()
        => Rollback(false);

    public ValueTask RollbackAsync(CancellationToken cancellationToken)
        => RollbackAsync(false, cancellationToken);

    #region disposable

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
                    _logger.LogTransactionTaskNotFinished(Guid, _task.Status, _task.IsCanceled, _task.IsCompleted, _task.IsFaulted);
                }
            }
            _queue.Writer.TryComplete();
            _cancellation.Dispose();
            // try { _task.Dispose(); } catch { }
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
                using var rollbackCancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(250));
                try
                {
                    await RollbackAsync(true, rollbackCancellation.Token).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
#if NET8_0_OR_GREATER
                    await _cancellation.CancelAsync().ConfigureAwait(false);
#else
                    _cancellation.Cancel();
#endif
                }
                catch (Exception exn)
                {
                    _logger.LogTransactionUnexpectedExceptionOnDispose(exn, Guid);
                }
            }
            if (!await WaitNoThrowAsync(timeout: TimeSpan.FromMilliseconds(20)).ConfigureAwait(false))
            {
#if NET8_0_OR_GREATER
                await _cancellation.CancelAsync().ConfigureAwait(false);
#else
                _cancellation.Cancel();
#endif
                if (!await WaitNoThrowAsync(timeout: TimeSpan.FromMilliseconds(200)).ConfigureAwait(false))
                {
                    // NOTE: if _task is null then WaitNoThrowAsync returns immideiately with true
                    _logger.LogTransactionTaskNotFinished(Guid, _task!.Status, _task.IsCanceled, _task.IsCompleted, _task.IsFaulted);
                }
            }
            _queue.Writer.TryComplete();
            _cancellation.Dispose();
            // try { _task.Dispose(); } catch { }
        }
    }

    #endregion
}