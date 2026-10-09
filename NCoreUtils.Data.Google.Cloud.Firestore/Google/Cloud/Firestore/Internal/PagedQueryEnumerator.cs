using Google.Cloud.Firestore;
using static NCoreUtils.Data.Google.Cloud.Firestore.Internal.FlowHelpers;

namespace NCoreUtils.Data.Google.Cloud.Firestore.Internal;

public sealed class PagedQueryEnumerator(
    Query query,
    Transaction? tx,
    int pageSize,
    CancellationToken cancellationToken)
    : IAsyncEnumerator<DocumentSnapshot>
{
    private DocumentSnapshot? _lastItem;

    private IEnumerator<DocumentSnapshot>? _snapshotEnumerator;

    public DocumentSnapshot Current { get; private set; } = default!;

    public ValueTask DisposeAsync()
    {
        _snapshotEnumerator?.Dispose();
        return default;
    }

    private Task<QuerySnapshot> GetSnapshotsAsync(Query query)
    {
        return tx is null
            ? query.GetSnapshotAsync(cancellationToken)
            : tx.GetSnapshotAsync(query, cancellationToken);
    }

    public async ValueTask<bool> MoveNextAsync()
    {
        // consume actual page if any
        if (_snapshotEnumerator is not null)
        {
            if (_snapshotEnumerator.MoveNext())
            {
                _lastItem = Current = _snapshotEnumerator.Current;
                return true;
            }
            _snapshotEnumerator = default;
        }
        // fetch next page
        var q = query;
        if (Exchange(ref _lastItem, null) is DocumentSnapshot lastItem)
        {
            q = q.StartAt(lastItem);
        }
        q = q.Limit(pageSize);
        // FIXME: apply selection
        var snapshot = await GetSnapshotsAsync(q).ConfigureAwait(false);
        var snapshotEnumerator = snapshot.GetEnumerator();
        if (!snapshotEnumerator.MoveNext())
        {
            return false;
        }
        _lastItem = Current = snapshotEnumerator.Current;
        _snapshotEnumerator = snapshotEnumerator;
        return true;
    }
}

public sealed class PagedQueryEnumerable(
    Query query,
    Transaction? tx,
    int pageSize = 10)
    : IAsyncEnumerable<DocumentSnapshot>
{
    public IAsyncEnumerator<DocumentSnapshot> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        => new PagedQueryEnumerator(query, tx, pageSize, cancellationToken);
}