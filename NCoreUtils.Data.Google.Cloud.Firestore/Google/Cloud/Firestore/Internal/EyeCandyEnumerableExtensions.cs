using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using Google.Cloud.Firestore;

namespace NCoreUtils.Data.Google.Cloud.Firestore.Internal;

internal static class EyeCandyEnumerableExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IAsyncEnumerable<T> MatreializeAsync<T>(
        this IAsyncEnumerable<DocumentSnapshot> documents,
        FirestoreMaterializer materializer,
        Expression<Func<DocumentSnapshot, T>> selector)
        => materializer.Materialize(documents, selector);
}