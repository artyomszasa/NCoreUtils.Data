using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using Google.Cloud.Firestore;
using Microsoft.Extensions.Logging;
using NCoreUtils.Data.Google.Cloud.Firestore.Expressions;
using NCoreUtils.Data.Google.Cloud.Firestore.Internal;
using NCoreUtils.Data.Internal;
using NCoreUtils.Data.Mapping;

namespace NCoreUtils.Data.Google.Cloud.Firestore;

public partial class FirestoreQueryProvider : QueryProviderBase
{
    private static async IAsyncEnumerable<DocumentSnapshot> MergeStream(
        PrefetchAsyncEnumerator<DocumentSnapshot>[] enumerators,
        IComparer<DocumentSnapshot> comparer,
        int offset,
        int? limit)
    {
        var consumed = 0;
        // NOTE: split query my result in duplicates --> elemets matched by more than one query
        // To handle this we track emitted item ids and skip duplicates
        var consumedIds = new HashSet<string>();
        var candidates = enumerators.MapToArray(static _ => new Maybe<DocumentSnapshot>());
        while (!limit.HasValue || consumed < offset + limit.Value)
        {
            // fill
            for (var i = 0; i < candidates.Length; ++i)
            {
                candidates[i] = await enumerators[i].GetCurrentAsync().ConfigureAwait(false);
            }
            // select
            var selectedIndex = -1;
            DocumentSnapshot? selectedValue = default;
            for (var i = 0; i < candidates.Length; ++i)
            {
                if (candidates[i].TryGetValue(out var doc))
                {
                    // first candidate or another value "comes earlier"
                    if (selectedValue is null || -1 == comparer.Compare(doc!, selectedValue))
                    {

                        selectedIndex = i;
                        selectedValue = doc;
                    }
                }
            }
            // check out of candidates
            if (selectedValue is null)
            {
                yield break;
            }
            // consume and yield the value (if not already yield)
            enumerators[selectedIndex].Consume();
            if (consumedIds.Add(selectedValue.Id))
            {
                if (consumed++ >= offset)
                {
                    yield return selectedValue;
                }
            }
        }
    }

    protected ILogger Logger { get; }

    protected IFirestoreConfiguration Configuration { get; }

    protected FirestoreModel Model { get; }

    protected IFirestoreDbAccessor DbAccessor { get; }

    protected internal FirestoreMaterializer Materializer { get; }

    public FirestoreQueryProvider(
        ILogger<FirestoreQueryProvider> logger,
        IFirestoreConfiguration configuration,
        FirestoreModel model,
        IFirestoreDbAccessor dbAccessor,
        FirestoreMaterializer materializer)
    {
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        Model = model ?? throw new ArgumentNullException(nameof(model));
        DbAccessor = dbAccessor ?? throw new ArgumentNullException(nameof(dbAccessor));
        Materializer = materializer ?? throw new ArgumentNullException(nameof(materializer));
    }

    [SuppressMessage("Performance", "CA1848:Use the LoggerMessage delegates", Justification = "TODO")]
    protected virtual void LogFirestoreQuery(FirestoreQuery query)
    {
        if (Logger.IsEnabled(LogLevel.Debug))
        {
            Preconditions.ThrowIfNull(query);
            Logger.LogDebug(
                "Executing firestore query: {{ Collection = {Collection}, Conditions = [{Conditions}], Ordering = [{Ordering}], Offset = {Offset}, Limit = {Limit} }}.",
                query.Collection,
                string.Join(", ", query.Conditions),
                string.Join(", ", query.Ordering),
                query.Offset,
                query.Limit
            );
        }
    }

    [SuppressMessage("Performance", "CA1822", Justification = "Backward compatibility.")]
    protected FirestoreQuery<TElement> Cast<TElement>(IQueryable<TElement> source)
        => source as FirestoreQuery<TElement> ?? throw new InvalidOperationException($"Unable to cast {source} to firestore query.");

    protected FirestoreQuery<TElement> ApplyOrdering<TElement, TKey>(FirestoreQuery<TElement> source, Expression<Func<TElement, TKey>> selector, FirestoreOrderingDirection direction)
    {
        var q = Cast(source);
        var chainedSelector = q.Selector.ChainSimplified(selector, true);
        if (TryResolvePath(chainedSelector.Body, q.Selector.Parameters[0], out var path, out var _))
        {
            return q.AddOrdering(new FirestoreOrdering(path, direction));
        }
        throw new InvalidOperationException($"Unable to resolve firestore field path for {selector}.");
    }

    protected virtual bool TryCreateFilteredQuery(
        FirestoreDb db,
        [NotNull] FirestoreQuery source,
        [MaybeNullWhen(false)] out Query query,
        out FirestoreMultiQuery multiQuery)
    {
        Preconditions.ThrowIfNull(db);
        ValidateQuery(source);
        if (SplitConditionsIfRequired(source, out multiQuery))
        {
            query = default!;
            return false;
        }
        query = db.Collection(source.Collection);
        foreach (var condition in source.Conditions)
        {
            query = condition.Apply(query, Configuration, source.Collection);
        }
        return true;
    }

    protected virtual bool TryCreateUnboundQuery(
        FirestoreDb db,
        [NotNull] FirestoreQuery source,
        [MaybeNullWhen(false)] out Query query,
        out FirestoreMultiQuery multiQuery)
    {
        if (TryCreateFilteredQuery(db, source, out query, out multiQuery))
        {
            var singleCondition = source.Conditions.Count == 1
                ? source.Conditions.First()
                : default(FirestoreCondition?);
            if (singleCondition is FirestoreCondition c && c.Operation == FirestoreCondition.Op.EqualTo && c.Path.Equals(FieldPath.DocumentId))
            {
                // If the condition is __key__ == xxxx then ordering is ignored completely
                return true;
            }
            foreach (var rule in source.Ordering)
            {
                query = rule.Direction switch
                {
                    FirestoreOrderingDirection.Ascending => query.OrderBy(rule.Path),
                    FirestoreOrderingDirection.Descending => query.OrderByDescending(rule.Path),
                    _ => throw new InvalidOperationException($"Invalid ordering direction {rule.Direction}.")
                };
            }
            return true;
        }
        query = default!;
        return false;
    }

    protected virtual IComparer<DocumentSnapshot> CreateDocumentComparer(FirestoreQuery query)
    {
        Preconditions.ThrowIfNull(query);
        IComparer<DocumentSnapshot>? comparer = default;
        foreach (var by in query.Ordering)
        {
            if (comparer is null)
            {
                comparer = new DocumentSnapshotByKeyComparer(by.Path, by.Direction == FirestoreOrderingDirection.Descending);
            }
            else
            {
                comparer = new NestedDocumentSnapshotByKeyComparer(comparer, by.Path, by.Direction == FirestoreOrderingDirection.Descending);
            }
        }
        return comparer ?? DocumentSnapshotByKeyComparer.ById;
    }

    /// <summary>
    /// Creates query from parameter. Only conditions are applied.
    /// </summary>
    /// <param name="source">Source query.</param>
    /// <returns>Firestore query with conditions applied</returns>
    [Obsolete("TryCreateFilteredQuery should be used")]
    protected Query CreateFilteredQuery(FirestoreDb db, FirestoreQuery source)
    {
        if (TryCreateFilteredQuery(db, source, out var query, out var _))
        {
            return query;
        }
        throw new InvalidOperationException("Query cannot be executed and must be split into multiple queries.");
    }

    /// <summary>
    /// Creates query from parameter. Only conditions and ordering are applied.
    /// </summary>
    /// <param name="source">Source query.</param>
    /// <returns>Firestore query with conditions and ordering applied.</returns>
    [Obsolete("TryCreateUnboundQuery should be used")]
    protected Query CreateUnboundQuery(FirestoreDb db, FirestoreQuery source)
    {
        if (TryCreateUnboundQuery(db, source, out var query, out var _))
        {
            return query;
        }
        throw new InvalidOperationException("Query cannot be executed and must be split into multiple queries.");
    }

    protected override IQueryable<TResult> ApplyOfType<TElement, TResult>(IQueryable<TElement> source)
    {
        throw new NotImplementedException("WIP (polymorphism)");
    }

    protected override IOrderedQueryable<TElement> ApplyOrderBy<TElement, TKey>(IQueryable<TElement> source, Expression<Func<TElement, TKey>> selector)
        => ApplyOrdering(Cast(source), selector, FirestoreOrderingDirection.Ascending);

    protected override IOrderedQueryable<TElement> ApplyOrderByDescending<TElement, TKey>(IQueryable<TElement> source, Expression<Func<TElement, TKey>> selector)
        => ApplyOrdering(Cast(source), selector, FirestoreOrderingDirection.Descending);

    protected override IQueryable<TResult> ApplySelect<TSource, TResult>(IQueryable<TSource> source, Expression<Func<TSource, TResult>> selector)
        => Cast(source).ApplySelector(selector);

    protected override IQueryable<TResult> ApplySelect<TSource, TResult>(IQueryable<TSource> source, Expression<Func<TSource, int, TResult>> selector)
        => throw new NotImplementedException("WIP (indexed select).");

    protected override IQueryable<TElement> ApplySkip<TElement>(IQueryable<TElement> source, int count)
        => Cast(source).ApplyOffset(count);

    protected override IQueryable<TElement> ApplyTake<TElement>(IQueryable<TElement> source, int count)
        => Cast(source).ApplyLimit(count);

    protected override IOrderedQueryable<TElement> ApplyThenBy<TElement, TKey>(IQueryable<TElement> source, Expression<Func<TElement, TKey>> selector)
        => ApplyOrdering(Cast(source), selector, FirestoreOrderingDirection.Ascending);

    protected override IOrderedQueryable<TElement> ApplyThenByDescending<TElement, TKey>(IQueryable<TElement> source, Expression<Func<TElement, TKey>> selector)
        => ApplyOrdering(Cast(source), selector, FirestoreOrderingDirection.Descending);

    protected override IQueryable<TElement> ApplyWhere<TElement>(IQueryable<TElement> source, Expression<Func<TElement, bool>> predicate)
    {
        var q = Cast(source);
        var chainedPredicate = q.Selector.ChainSimplified(predicate, true);
        return q.AddConditions(ExtractConditions(chainedPredicate));
    }

    protected override IQueryable<TElement> ApplyWhere<TElement>(IQueryable<TElement> source, Expression<Func<TElement, int, bool>> predicate)
        => throw new NotImplementedException("WIP (indexed where).");

    protected override Task<bool> ExecuteAll<TElement>(IQueryable<TElement> source, Expression<Func<TElement, bool>> predicate, CancellationToken cancellationToken)
        => throw new NotSupportedException("Executing .All(...) would result in querying all entities.");

    protected override Task<bool> ExecuteAny<TElement>(IQueryable<TElement> source, CancellationToken cancellationToken)
    {
        var q = Cast(source);
        if (q.IsAlwaysFalse())
        {
            return Task.FromResult(false);
        }
        return DbAccessor.ExecuteAsync(async (db, tx, cancellationToken) =>
        {
            if (TryCreateUnboundQuery(db, q, out var query, out var mq))
            {
                return await DoExecuteAny(this, tx, query, q, cancellationToken).ConfigureAwait(false);
            }
            // If query has been split true is returned if any of the split queries returns true.
            foreach (var q1 in mq.Queries)
            {
                if (!TryCreateUnboundQuery(db, q1, out query, out var _))
                {
                    throw new InvalidOperationException("Should never happen (query has already been splitted).");
                }
                if (await DoExecuteAny(this, tx, query, q1, cancellationToken).ConfigureAwait(false))
                {
                    return true;
                }
            }
            return false;
        }, cancellationToken);

        static async Task<bool> DoExecuteAny(FirestoreQueryProvider self, Transaction? tx, Query query, FirestoreQuery q, CancellationToken cancellationToken)
        {
            query = query.Limit(1);
            self.LogFirestoreQuery(q);
            var snapshot = tx is null
                ? await query.GetSnapshotAsync(cancellationToken).ConfigureAwait(false)
                : await tx.GetSnapshotAsync(query, cancellationToken).ConfigureAwait(false);
            return snapshot.Count > 0;
        }
    }

    protected override Task<int> ExecuteCount<TElement>(IQueryable<TElement> source, CancellationToken cancellationToken)
#if NET6_0_OR_GREATER
        => DbAccessor.ExecuteAsync(
            async (db, tx, cancellationToken) => (int)(tx is null
                ? await CountQueryAsync(db, Cast(source), cancellationToken).ConfigureAwait(false)
                : await CountQueryAsync(db, tx, Cast(source), cancellationToken).ConfigureAwait(false)),
            cancellationToken
        );
#else
        => throw new NotSupportedException("Executing .Count(...) would result in querying all entities.");
#endif

    protected override Task<TElement> ExecuteFirst<TElement>(IQueryable<TElement> source, CancellationToken cancellationToken)
        => ExecuteQuery(source.Skip(0).Take(1)).FirstAsync(cancellationToken).AsTask();

    protected override Task<TElement> ExecuteFirstOrDefault<TElement>(IQueryable<TElement> source, CancellationToken cancellationToken)
        => ExecuteQuery(source.Skip(0).Take(1)).FirstOrDefaultAsync(cancellationToken).AsTask()!;

    protected override Task<TElement> ExecuteLast<TElement>(IQueryable<TElement> source, CancellationToken cancellationToken)
        => ExecuteFirst(Cast(source).ReverseOrder(), cancellationToken);

    protected override Task<TElement> ExecuteLastOrDefault<TElement>(IQueryable<TElement> source, CancellationToken cancellationToken)
        => ExecuteFirstOrDefault(Cast(source).ReverseOrder(), cancellationToken);

    protected override async Task<TElement> ExecuteSingle<TElement>(IQueryable<TElement> source, CancellationToken cancellationToken)
    {
        var items = await ExecuteQuery(source.Skip(0).Take(2)).ToListAsync(cancellationToken).ConfigureAwait(false);
        return items.Count switch
        {
            0 => throw new InvalidOperationException("Sequence contains no elements."),
            1 => items[0],
            _ => throw new InvalidOperationException("Sequence contains multiple elements."),
        };
    }

    protected override async Task<TElement> ExecuteSingleOrDefault<TElement>(IQueryable<TElement> source, CancellationToken cancellationToken)
    {
        var items = await ExecuteQuery(source.Skip(0).Take(2)).ToListAsync(cancellationToken).ConfigureAwait(false);
        return items.Count switch
        {
            0 => default!,
            1 => items[0],
            _ => throw new InvalidOperationException("Sequence contains multiple elements."),
        };
    }

#if NET6_0_OR_GREATER
    protected virtual async Task<long> CountQueryAsync<TElement>(
        FirestoreDb db,
        FirestoreQuery<TElement> q,
        CancellationToken cancellationToken)
    {
        if (TryCreateUnboundQuery(db, q, out var query, out _))
        {
            var snapshot = await query.Count().GetSnapshotAsync(cancellationToken).ConfigureAwait(false);
            return snapshot.Count ?? 0;
        }
        throw new NotSupportedException("Only simple (natively supported by Firestore) queries are supported.");
    }

    protected virtual async Task<long> CountQueryAsync<TElement>(
        FirestoreDb db,
        Transaction tx,
        FirestoreQuery<TElement> q,
        CancellationToken cancellationToken)
    {
        Preconditions.ThrowIfNull(tx);
        if (TryCreateUnboundQuery(db, q, out var query, out _))
        {
            var snapshot = await tx.GetSnapshotAsync(query.Count(), cancellationToken).ConfigureAwait(false);
            return snapshot.Count ?? 0;
        }
        throw new NotSupportedException("Only simple (natively supported by Firestore) queries are supported.");
    }
#endif

    protected virtual async IAsyncEnumerable<DocumentSnapshot> FetchInsideTransactionAsync<TElement>(
        FirestoreDb db,
        Transaction tx,
        FirestoreQuery<TElement> q,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        Preconditions.ThrowIfNull(db);
        Preconditions.ThrowIfNull(tx);
        Preconditions.ThrowIfNull(q);
        if (TryCreateUnboundQuery(db, q, out var query, out var mq))
        {
            if (q.Offset > 0)
            {
                query = query.Offset(q.Offset);
            }
            if (q.Limit.HasValue)
            {
                query = query.Limit(q.Limit.Value);
            }
            // apply fields selection
            var normalFieldPaths = q.Selector.CollectFirestorePaths();
            var selectPaths = q.ShadowFields.Count == 0
                ? normalFieldPaths.ToArray()
                : [.. normalFieldPaths, .. q.ShadowFields];
            query = query.Select(selectPaths);
            LogFirestoreQuery(q);
            var snapshot = await query.GetSnapshotAsync(cancellationToken).ConfigureAwait(false);
            foreach (var item in snapshot)
            {
                yield return item;
            }
        }
        // when query is split we execute all queries concurrently maintaining order of the results
        // offset and limit are applied on the resulting enumeration on the client side.
        var enumerators = mq.Queries.MapToArray(q1 =>
        {
            if (!TryCreateUnboundQuery(db, q1, out var query, out _))
            {
                throw new InvalidOperationException("Should never happen (query has already been split).");
            }
            // FIXME: apply selection
            LogFirestoreQuery(q1);
            return new PrefetchAsyncEnumerator<DocumentSnapshot>(
                new PagedQueryEnumerable(query, tx, pageSize: 10),
                cancellationToken
            );
        });
        await foreach (var item in MergeStream(enumerators, CreateDocumentComparer(q), q.Offset, q.Limit).ConfigureAwait(false))
        {
            yield return item;
        }
    }

    protected virtual IAsyncEnumerable<DocumentSnapshot> StreamQueryAsync<TElement>(
        FirestoreDb db,
        FirestoreQuery<TElement> q,
        CancellationToken cancellationToken)
    {
        if (TryCreateUnboundQuery(db, q, out var query, out var mq))
        {
            if (q.Offset > 0)
            {
                query = query.Offset(q.Offset);
            }
            if (q.Limit.HasValue)
            {
                query = query.Limit(q.Limit.Value);
            }
            // apply fields selection
            var normalFieldPaths = q.Selector.CollectFirestorePaths();
            var selectPaths = q.ShadowFields.Count == 0
                ? normalFieldPaths.ToArray()
                : [.. normalFieldPaths, .. q.ShadowFields];
            query = query.Select(selectPaths);
            LogFirestoreQuery(q);
            return query.StreamAsync(cancellationToken);
        }
        // when query is split we execute all queries concurrently maintaining order of the results
        // offset and limit are applied on the resulting enumeration on the client side.
        var enumerators = mq.Queries.MapToArray(q1 =>
        {
            if (!TryCreateUnboundQuery(db, q1, out var query, out _))
            {
                throw new InvalidOperationException("Should never happen (query has already been split).");
            }
            if (q1.Limit.HasValue)
            {
                // also include elements to skip as ordering is performed on the client side
                query = query.Limit(q1.Offset + q1.Limit.Value);
            }
            // FIXME: apply selection
            LogFirestoreQuery(q1);
            return new PrefetchAsyncEnumerator<DocumentSnapshot>(query.StreamAsync(cancellationToken), cancellationToken);
        });
        return MergeStream(enumerators, CreateDocumentComparer(q), q.Offset, q.Limit);
    }

    protected override IAsyncEnumerable<TElement> ExecuteQuery<TElement>(IQueryable<TElement> source)
    {
        var q = Cast(source);
        if (q.IsAlwaysFalse())
        {
            return EmptyAsyncEnumerable<TElement>.Singleton;
        }
        return LogExecution(new DelayedAsyncEnumerable<TElement>(cancellationToken => new ValueTask<IAsyncEnumerable<TElement>>(
            DbAccessor.ExecuteAsync(async (db, tx, cancellationToken) =>
            {
                if (tx is not null)
                {
                    var items = await FetchInsideTransactionAsync(db, tx, q, cancellationToken)
                        .MatreializeAsync(Materializer, q.Selector)
                        .ToArrayAsync(cancellationToken)
                        .ConfigureAwait(false);
                    return items.ToAsyncEnumerable();
                }
                var stream = StreamQueryAsync(db, q, cancellationToken);
                return Materializer.Materialize(stream, q.Selector);
            }, cancellationToken)
        )));
    }

    /// <summary>
    /// Determines whether conditions of the specified query can be handled within single query. If not creates
    /// multiple queries with conditions split into multiple queries.
    /// </summary>
    /// <param name="query">Query to check.</param>
    /// <param name="queries">When query must be split the resulting query collection.</param>
    /// <returns>
    /// <c>true</c> if query has been split, <c>false</c> otherwise.
    /// </returns>
    protected virtual bool SplitConditionsIfRequired(FirestoreQuery query, out FirestoreMultiQuery queries)
    {
        Preconditions.ThrowIfNull(query);
        if (query.Conditions.TryGetFirst(c => c.Operation == FirestoreCondition.Op.ArrayContainsAny, out var c))
        {
            var wrapper = Model.GetCollectionWrapperFactory().Create(c.Value!);
            if (wrapper.Count > 10)
            {
                // FIXME: pool
                var values = new List<object>((wrapper.Count - 1) / 10 + 1);
                wrapper.SplitIntoChunks(10, values);
                queries = new FirestoreMultiQuery(values.MapToArray(newValue =>
                {
                    var conditions = ImmutableHashSet.CreateBuilder<FirestoreCondition>();
                    foreach (var condition in query.Conditions)
                    {
                        if (condition.Operation == FirestoreCondition.Op.ArrayContainsAny)
                        {
                            conditions.Add(new FirestoreCondition(condition.Path, condition.Operation, newValue));
                        }
                        else
                        {
                            conditions.Add(condition);
                        }
                    }
                    return query.ReplaceConditions(conditions.ToImmutable());
                }));
                return true;
            }
        }
        queries = default;
        return false;
    }

    /// <summary>
    /// Validates query. Throws on invalid cases (e.g. multiple ContainsAny conditions) so these cases may be
    /// unhandled in further processing.
    /// </summary>
    /// <param name="query">Query to validate.</param>
    protected virtual void ValidateQuery([NotNull] FirestoreQuery query)
    {
        Preconditions.ThrowIfNull(query);
        if (query.Conditions.Count(c => c.Operation == FirestoreCondition.Op.ArrayContainsAny) > 1)
        {
            throw new InvalidOperationException("Firestore query may only include single ArrayContainsAny condition.");
        }
    }
}