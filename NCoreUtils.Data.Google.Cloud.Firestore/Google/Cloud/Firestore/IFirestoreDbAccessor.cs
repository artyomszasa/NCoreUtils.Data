using Google.Cloud.Firestore;

namespace NCoreUtils.Data.Google.Cloud.Firestore;

public interface IFirestoreDbAccessor
{
    Task ExecuteAsync(
        Func<FirestoreDb, Transaction?, CancellationToken, Task> action,
        CancellationToken cancellationToken
    );

    Task<T> ExecuteAsync<T>(
        Func<FirestoreDb, Transaction?, CancellationToken, Task<T>> action,
        CancellationToken cancellationToken
    );
}