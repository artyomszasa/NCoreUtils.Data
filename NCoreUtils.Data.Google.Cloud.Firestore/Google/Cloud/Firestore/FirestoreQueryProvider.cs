using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Google.Cloud.Firestore;
using Microsoft.Extensions.Logging;
using NCoreUtils.Data.Google.Cloud.Firestore.Expressions;
using NCoreUtils.Data.Google.Cloud.Firestore.Internal;
using NCoreUtils.Data.Internal;
using NCoreUtils.Data.Mapping;

namespace NCoreUtils.Data.Google.Cloud.Firestore
{
    public partial class FirestoreQueryProvider : QueryProviderBase
    {
        protected ILogger Logger { get; }

        protected IFirestoreConfiguration Configuration { get; }

        protected FirestoreModel Model { get; }

        protected IFirestoreDbAccessor DbAccessor { get; }

        protected FirestoreMaterializer Materializer { get; }

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

        protected virtual void LogFirestoreQuery(FirestoreQuery query)
        {
            if (Logger.IsEnabled(LogLevel.Debug))
            {
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
            FirestoreQuery source,
            [MaybeNullWhen(false)] out Query query,
            out FirestoreMultiQuery multiQuery)
        {
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
            FirestoreQuery source,
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
            return DbAccessor.ExecuteAsync(async db =>
            {
                if (TryCreateUnboundQuery(db, q, out var query, out var mq))
                {
                    return await DoExecuteAny(this, query, q, cancellationToken);
                }
                // If query has been split true is returned if any of the split queries returns true.
                foreach (var q1 in mq.Queries)
                {
                    if (!TryCreateUnboundQuery(db, q1, out query, out var _))
                    {
                        throw new InvalidOperationException("Should never happen (query has already been splitted).");
                    }
                    if (await DoExecuteAny(this, query, q1, cancellationToken))
                    {
                        return true;
                    }
                }
                return false;
            });

            static async Task<bool> DoExecuteAny(FirestoreQueryProvider self, Query query, FirestoreQuery q, CancellationToken cancellationToken)
            {
                query.Limit(1);
                self.LogFirestoreQuery(q);
                var snapshot = await query.GetSnapshotAsync(cancellationToken);
                return snapshot.Count > 0;
            }
        }

        protected override Task<int> ExecuteCount<TElement>(IQueryable<TElement> source, CancellationToken cancellationToken)
            => throw new NotSupportedException("Executing .Count(...) would result in querying all entities.");

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
            var items = await ExecuteQuery(source.Skip(0).Take(2)).ToListAsync(cancellationToken);
            return items.Count switch
            {
                0 => throw new InvalidOperationException("Sequence contains no elements."),
                1 => items[0],
                _ => throw new InvalidOperationException("Sequence contains multiple elements."),
            };
        }

        protected override async Task<TElement> ExecuteSingleOrDefault<TElement>(IQueryable<TElement> source, CancellationToken cancellationToken)
        {
            var items = await ExecuteQuery(source.Skip(0).Take(2)).ToListAsync(cancellationToken);
            return items.Count switch
            {
                0 => default!,
                1 => items[0],
                _ => throw new InvalidOperationException("Sequence contains multiple elements."),
            };
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
                query = query.Select(q.Selector.CollectFirestorePaths().ToArray());
                LogFirestoreQuery(q);
                return query.StreamAsync(cancellationToken);
            }
            // when query is split we execute all queries concurrently maintaining order of the results
            // offset and limit are applied on the resulting enumeration on the client side.
            var enumerators = mq.Queries.MapToArray(q1 =>
            {
                if (!TryCreateUnboundQuery(db, q1, out var query, out var mq))
                {
                    throw new InvalidOperationException("Should never happen (query has already been splitted).");
                }
                if (q1.Limit.HasValue)
                {
                    // also include elements to skip as ordering is performed on the client side
                    query = query.Limit(q1.Offset + q1.Limit.Value);
                }
                LogFirestoreQuery(q1);
                return new PrefetchAsyncEnumerator<DocumentSnapshot>(query.StreamAsync(cancellationToken), cancellationToken);
            });
            return MergeStream(enumerators, CreateDocumentComparer(q), q.Offset, q.Limit);

            static async IAsyncEnumerable<DocumentSnapshot> MergeStream(
                PrefetchAsyncEnumerator<DocumentSnapshot>[] enumerators,
                IComparer<DocumentSnapshot> comparer,
                int offset,
                int? limit)
            {
                var consumed = 0;
                var candidates = enumerators.MapToArray(_ => new Maybe<DocumentSnapshot>());
                while (!limit.HasValue || consumed < offset + limit.Value)
                {
                    // fill
                    for (var i = 0; i < candidates.Length; ++i)
                    {
                        candidates[i] = await enumerators[i].GetCurrentAsync();
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
                    // consume and yield the value
                    enumerators[selectedIndex].Consume();
                    ++consumed;
                    if (consumed - offset >= 0)
                    {
                        yield return selectedValue;
                    }
                }
            }
        }

        protected override IAsyncEnumerable<TElement> ExecuteQuery<TElement>(IQueryable<TElement> source)
        {
            var q = Cast(source);
            if (q.IsAlwaysFalse())
            {
                return new EmptyAsyncEnumerable<TElement>();
            }
            return new DelayedAsyncEnumerable<TElement>(cancellationToken => new ValueTask<IAsyncEnumerable<TElement>>(DbAccessor.ExecuteAsync(db =>
            {
                var stream = StreamQueryAsync(db, q, cancellationToken);
                return Task.FromResult(Materializer.Materialize(stream, q.Selector));
            })));


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
            if (query.Conditions.TryGetFirst(c => c.Operation == FirestoreCondition.Op.ArrayContainsAny, out var c))
            {
                var wrapper = CollectionWrapper.Create(c.Value!);
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
        protected virtual void ValidateQuery(FirestoreQuery query)
        {
            if (query.Conditions.Count(c => c.Operation == FirestoreCondition.Op.ArrayContainsAny) > 1)
            {
                throw new InvalidOperationException("Firestore query may only include single ArrayCOntainsAny condition.");
            }
        }
    }
}