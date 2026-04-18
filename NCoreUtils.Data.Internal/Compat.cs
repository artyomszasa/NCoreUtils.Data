namespace NCoreUtils.Data.Internal;

internal static class Compat
{
    private static void DoAwait(this ValueTask source)
    {
        if (source.IsCompletedSuccessfully)
        {
            return;
        }
        source.AsTask().Wait();
    }

    private static T DoAwait<T>(this ValueTask<T> source)
    {
        if (source.IsCompletedSuccessfully)
        {
            return source.Result;
        }
        return source.AsTask().GetAwaiter().GetResult();
    }

    public static IEnumerable<T> ToEnumerable<T>(this IAsyncEnumerable<T> source)
    {
        ArgumentNullException.ThrowIfNull(source);
        var enumerator = source.GetAsyncEnumerator(CancellationToken.None);
        try
        {
            while (enumerator.MoveNextAsync().DoAwait())
            {
                yield return enumerator.Current;
            }
        }
        finally
        {
            enumerator.DisposeAsync().DoAwait();
        }
    }
}