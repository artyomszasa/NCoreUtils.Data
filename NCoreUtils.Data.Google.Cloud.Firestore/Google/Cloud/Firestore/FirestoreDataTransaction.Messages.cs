using Google.Cloud.Firestore;

namespace NCoreUtils.Data.Google.Cloud.Firestore;

public partial class FirestoreDataTransaction
{
    private abstract class Message
    {
        public static CommitMessage Commit { get; } = new CommitMessage();

        public static RollbackMessage Rollback { get; } = new RollbackMessage();

        public static CommitAsyncMessage CommitAsync() => new();

        public static RollbackAsyncMessage RollbackAsync() => new();

        public static ActionMessage<T> Action<T>(Func<Transaction, Task<T>> action) => new(action);

        protected Message() { }

        public abstract ValueTask<bool> RunAsync(Transaction tx);
    }

    private class CommitMessage : Message
    {
        public override ValueTask<bool> RunAsync(Transaction tx)
        {
            return new ValueTask<bool>(true);
        }

        public override string ToString() => "Commit";
    }

    private class RollbackMessage : Message
    {
        public override ValueTask<bool> RunAsync(Transaction tx)
        {
            throw new AbortTransactionException();
        }

        public override string ToString() => "Rollback";
    }

    private class CommitAsyncMessage : Message
    {
#if NETSTANDARD2_1
        public TaskCompletionSource<int> Completion { get; } = new();
#else
        public TaskCompletionSource Completion { get; } = new();
#endif

        public override ValueTask<bool> RunAsync(Transaction tx)
        {
#if NETSTANDARD2_1
            Completion.TrySetResult(default);
#else
            Completion.TrySetResult();
#endif
            return new ValueTask<bool>(true);
        }

        public override string ToString() => "CommitAsync";
    }

    private class RollbackAsyncMessage : Message
    {
#if NETSTANDARD2_1
        public TaskCompletionSource<int> Completion { get; } = new();
#else
        public TaskCompletionSource Completion { get; } = new();
#endif

        public override ValueTask<bool> RunAsync(Transaction tx)
        {
#if NETSTANDARD2_1
            Completion.TrySetResult(default);
#else
            Completion.TrySetResult();
#endif
            throw new AbortTransactionException();
        }

        public override string ToString() => "RollbackAsync";
    }

    class ActionMessage<T>(Func<Transaction, Task<T>> action) : Message
    {
        public TaskCompletionSource<T> Completion { get; } = new TaskCompletionSource<T>();

        public Func<Transaction, Task<T>> Action { get; } = action ?? throw new ArgumentNullException(nameof(action));

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

        public override string ToString() => "Action";
    }
}