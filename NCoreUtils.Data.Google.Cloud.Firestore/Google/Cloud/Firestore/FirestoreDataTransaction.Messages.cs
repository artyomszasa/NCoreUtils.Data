using System;
using System.Threading.Tasks;
using Google.Cloud.Firestore;

namespace NCoreUtils.Data.Google.Cloud.Firestore;

public partial class FirestoreDataTransaction
{
    private abstract class Message
    {
        public static CommitMessage Commit { get; } = new CommitMessage();

        public static RollbackMessage Rollback { get; } = new RollbackMessage();

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