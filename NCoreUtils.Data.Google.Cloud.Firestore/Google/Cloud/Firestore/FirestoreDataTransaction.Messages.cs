using Google.Cloud.Firestore;
using NCoreUtils.Data.Google.Cloud.Firestore.Internal;

namespace NCoreUtils.Data.Google.Cloud.Firestore;

#if NETSTANDARD2_1

internal sealed class TaskCompletionSource(TaskCreationOptions options)
{
    private readonly TaskCompletionSource<int> _completion = new(options);

    public Task Task => _completion.Task;

    public bool TrySetCanceled() => _completion.TrySetCanceled();

    public bool TrySetCanceled(CancellationToken cancellationToken) => _completion.TrySetCanceled(cancellationToken);

    public bool TrySetException(IEnumerable<Exception> exceptions) => _completion.TrySetException(exceptions);

    public bool TrySetException(Exception exception) => _completion.TrySetException(exception);

    public bool TrySetResult() => _completion.TrySetResult(default);
}

#endif

public partial class FirestoreDataTransaction
{
    private abstract class Message
#if NET6_0_OR_GREATER
        : ISpanFormattable
#else
        : IFormattable
#endif
    {
        // public static CommitMessage Commit { get; } = new CommitMessage();

        // public static RollbackMessage Rollback { get; } = new RollbackMessage();

        public static CommitAsyncMessage CommitAsync() => new();

        public static RollbackAsyncMessage RollbackAsync() => new();

        public static ActionMessage<T> Action<T>(Func<Transaction, CancellationToken, Task<T>> action, CancellationToken cancellationToken)
            => new(action, cancellationToken);

        protected Message() { }

        public abstract void Abort();

        public abstract ValueTask<bool> RunAsync(FirestoreDataTransaction dtx, Transaction tx);

        public abstract bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider);

        public abstract string ToString(string? format, IFormatProvider? formatProvider);
    }

    // private class CommitMessage : Message
    // {
    //     public override ValueTask<bool> RunAsync(FirestoreDataTransaction dtx, Transaction tx)
    //     {
    //         return new ValueTask<bool>(true);
    //     }
//
    //     public override string ToString() => "Commit";
//
    //     public override string ToString(string? format, IFormatProvider? formatProvider)
    //         => ToString();
    // }

    // private class RollbackMessage : Message
    // {
    //     public override ValueTask<bool> RunAsync(FirestoreDataTransaction dtx, Transaction tx)
    //     {
    //         throw new AbortTransactionException();
    //     }
//
    //     public override string ToString() => "Rollback";
    // }

    private sealed class CommitAsyncMessage : Message
    {
        private const string DisplayString = "CommitAsync";

        public TaskCompletionSource Completion { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public override void Abort()
        {
            Completion.TrySetException(new InvalidOperationException("Execution inside a firebase trasnaction has been aborted."));
        }

        public override ValueTask<bool> RunAsync(FirestoreDataTransaction dtx, Transaction tx)
        {
            dtx.SetOnCompleted(Completion);
            return new ValueTask<bool>(true);
        }

        public override string ToString()
            => DisplayString;

        public override string ToString(string? format, IFormatProvider? formatProvider)
            => DisplayString;

        public override bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
            => DisplayString.TryCopyTo(destination, out charsWritten);
    }

    private sealed class RollbackAsyncMessage : Message
    {
        private const string DisplayString = "RollbackAsync";

        public TaskCompletionSource Completion { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public override void Abort()
        {
            Completion.TrySetException(new InvalidOperationException("Execution inside a firebase trasnaction has been aborted."));
        }

        public override ValueTask<bool> RunAsync(FirestoreDataTransaction dtx, Transaction tx)
        {
            dtx.SetOnFailed(Completion);
            throw new AbortTransactionException();
        }

        public override string ToString()
            => DisplayString;

        public override string ToString(string? format, IFormatProvider? formatProvider)
            => DisplayString;

        public override bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
            => DisplayString.TryCopyTo(destination, out charsWritten);
    }

    private sealed class ActionMessage<T>(
        Func<Transaction, CancellationToken, Task<T>> action,
        CancellationToken actionCancellationToken)
        : Message
    {
        private const string DisplayString = "Action";

        public TaskCompletionSource<T> Completion { get; } = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);

        public Func<Transaction, CancellationToken, Task<T>> Action { get; } = action ?? throw new ArgumentNullException(nameof(action));

        public CancellationToken ActionCancellationToken { get; } = actionCancellationToken;

        private async Task<T> ExecuteWithCompositeCancellationAsync(Transaction tx)
        {
            using var compositeCancellation = CancellationTokenSource.CreateLinkedTokenSource(
                ActionCancellationToken,
                tx.CancellationToken
            );
            return await Action(tx, compositeCancellation.Token).ConfigureAwait(false);
        }

        private Task<T> ExecuteAsync(Transaction tx)
        {
            if (ActionCancellationToken.CanBeCanceled)
            {
                return ExecuteWithCompositeCancellationAsync(tx);
            }
            return Action(tx, tx.CancellationToken);
        }

        public override void Abort()
        {
            Completion.TrySetException(new InvalidOperationException("Execution inside a firebase trasnaction has been aborted."));
        }

        public override async ValueTask<bool> RunAsync(
            FirestoreDataTransaction dtx,
            Transaction tx)
        {
            try
            {
                var result = await ExecuteAsync(tx).ConfigureAwait(false);
                Completion.TrySetResult(result);
                return false;
            }
            catch (Exception exn)
            {
                if (exn is OperationCanceledException cancelled)
                {
                    Completion.TrySetCanceled(cancelled.CancellationToken);
                }
                else
                {
                    Completion.TrySetException(exn);
                }
                throw new AbortTransactionException();
            }
        }

        public override string ToString()
            => DisplayString;

        public override string ToString(string? format, IFormatProvider? formatProvider)
            => DisplayString;

        public override bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
            => DisplayString.TryCopyTo(destination, out charsWritten);
    }
}