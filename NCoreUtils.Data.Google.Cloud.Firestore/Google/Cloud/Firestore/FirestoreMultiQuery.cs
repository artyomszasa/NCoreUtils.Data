using System.Collections.Generic;

namespace NCoreUtils.Data.Google.Cloud.Firestore;

public readonly struct FirestoreMultiQuery
{
    private static readonly IReadOnlyList<FirestoreQuery> _noQueries = System.Array.Empty<FirestoreQuery>();

    private readonly IReadOnlyList<FirestoreQuery>? _queries;

    public IReadOnlyList<FirestoreQuery> Queries
        => _queries ?? _noQueries;

    public bool IsDefault
        => _queries is null;

    public FirestoreMultiQuery(IReadOnlyList<FirestoreQuery> queries)
        => _queries = queries;
}