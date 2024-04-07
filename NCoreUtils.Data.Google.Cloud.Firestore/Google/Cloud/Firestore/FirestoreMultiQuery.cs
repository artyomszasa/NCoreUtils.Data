using System.Collections.Generic;

namespace NCoreUtils.Data.Google.Cloud.Firestore;

public readonly struct FirestoreMultiQuery(IReadOnlyList<FirestoreQuery> queries)
{
    private static readonly IReadOnlyList<FirestoreQuery> _noQueries = [];

    private readonly IReadOnlyList<FirestoreQuery>? _queries = queries;

    public IReadOnlyList<FirestoreQuery> Queries
        => _queries ?? _noQueries;

    public bool IsDefault
        => _queries is null;
}