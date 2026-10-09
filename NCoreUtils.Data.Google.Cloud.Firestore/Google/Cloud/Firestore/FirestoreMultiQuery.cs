namespace NCoreUtils.Data.Google.Cloud.Firestore;

[System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1815:Override equals and operator equals on value types", Justification = "Not relevant in this case")]
public readonly struct FirestoreMultiQuery(IReadOnlyList<FirestoreQuery> queries)
{
    private static readonly IReadOnlyList<FirestoreQuery> _noQueries = [];

    private readonly IReadOnlyList<FirestoreQuery>? _queries = queries;

    public IReadOnlyList<FirestoreQuery> Queries
        => _queries ?? _noQueries;

    public bool IsDefault
        => _queries is null;
}