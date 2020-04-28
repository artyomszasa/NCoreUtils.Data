using System;
using System.Threading.Tasks;
using Google.Cloud.Firestore;

namespace NCoreUtils.Data.Google.Cloud.Firestore
{
    public interface IFirestoreDbAccessor
    {
        Task ExecuteAsync(Func<FirestoreDb, Task> action);

        Task<T> ExecuteAsync<T>(Func<FirestoreDb, Task<T>> action);
    }
}