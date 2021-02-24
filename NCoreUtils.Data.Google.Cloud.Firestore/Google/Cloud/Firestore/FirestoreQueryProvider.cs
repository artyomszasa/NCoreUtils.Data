using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Google.Cloud.Firestore;
using Microsoft.Extensions.Logging;
using NCoreUtils.Data.Google.Cloud.Firestore.Expressions;
using NCoreUtils.Data.Internal;
using NCoreUtils.Data.Mapping;

namespace NCoreUtils.Data.Google.Cloud.Firestore
{
    public partial class FirestoreQueryProvider : QueryProviderBase
    {
        protected ILogger Logger { get; }

        protected FirestoreModel Model { get; }

        protected IFirestoreDbAccessor DbAccessor { get; }

        protected FirestoreMaterializer Materializer { get; }

        public FirestoreQueryProvider(ILogger<FirestoreQueryProvider> logger, FirestoreModel model, IFirestoreDbAccessor dbAccessor, FirestoreMaterializer materializer)
        {
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
            Model = model ?? throw new ArgumentNullException(nameof(model));
            DbAccessor = dbAccessor ?? throw new ArgumentNullException(nameof(dbAccessor));
            Materializer = materializer ?? throw new ArgumentNullException(nameof(materializer));
        }

        protected virtual void LogFirestoreQuery(FirestoreQuery query)
        {
            if (Logger.IsEnabled(LogLevel.Debug))
            {
                Logger.LogDebug(
                    "Executing firestore query: {{ Collection = {0}, Conditions = [{1}], Ordering = [{2}], Offset = {3}, Limit = {4} }}.",
                    query.Collection,
                    string.Join(", ", query.Conditions),
                    string.Join(", ", query.Ordering),
                    query.Offset,
                    query.Limit
                );
            }
        }

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

        /// <summary>
        /// Creates query from parameter. Only conditions are applied.
        /// </summary>
        /// <param name="source">Source query.</param>
        /// <returns>Firestore query with conditions applied</returns>
        protected Query CreateFilteredQuery(FirestoreDb db, FirestoreQuery source)
        {
            Query query = db.Collection(source.Collection);
            foreach (var condition in source.Conditions)
            {
                query = condition.Apply(query, source.Collection);
            }
            return query;
        }

        /// <summary>
        /// Creates query from parameter. Only conditions and ordering are applied.
        /// </summary>
        /// <param name="source">Source query.</param>
        /// <returns>Firestore query with conditions and ordering applied.</returns>
        protected Query CreateUnboundQuery(FirestoreDb db, FirestoreQuery source)
        {
            var query = CreateFilteredQuery(db, source);
            if (source.Conditions.Count == 1 && source.Conditions[0].Operation == FirestoreCondition.Op.EqualTo && source.Conditions[0].Path.Equals(FieldPath.DocumentId))
            {
                // If the condition is __key__ == xxxx then ordering is ignored completely
                return query;
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
            return query;
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
                var query = CreateFilteredQuery(db, q);
                query.Limit(1);
                LogFirestoreQuery(q);
                var snapshot = await query.GetSnapshotAsync(cancellationToken);
                return snapshot.Count > 0;
            });
        }

        protected override Task<int> ExecuteCount<TElement>(IQueryable<TElement> source, CancellationToken cancellationToken)
            => throw new NotSupportedException("Executing .Count(...) would result in querying all entities.");

        protected override Task<TElement> ExecuteFirst<TElement>(IQueryable<TElement> source, CancellationToken cancellationToken)
        {
            var q = Cast(source);
            if (q.IsAlwaysFalse())
            {
                throw new InvalidOperationException("Sequence contains no elements.");
            }
            return DbAccessor.ExecuteAsync(async db =>
            {
                var query = CreateUnboundQuery(db, q);
                query.Limit(1);
                LogFirestoreQuery(q);
                var snapshot = await query.GetSnapshotAsync(cancellationToken);
                if (snapshot.Count > 0)
                {
                    return Materializer.Materialize(snapshot[0], q.Selector);
                }
                throw new InvalidOperationException("Sequence contains no elements.");
            });
        }

        protected override Task<TElement> ExecuteFirstOrDefault<TElement>(IQueryable<TElement> source, CancellationToken cancellationToken)
        {
            var q = Cast(source);
            if (q.IsAlwaysFalse())
            {
                return Task.FromResult<TElement>(default!);
            }
            return DbAccessor.ExecuteAsync(async db =>
            {
                var query = CreateUnboundQuery(db, q);
                query.Limit(1);
                LogFirestoreQuery(q);
                var snapshot = await query.GetSnapshotAsync(cancellationToken);
                if (snapshot.Count > 0)
                {
                    return Materializer.Materialize(snapshot[0], q.Selector);
                }
                return default!;
            });
        }

        protected override Task<TElement> ExecuteLast<TElement>(IQueryable<TElement> source, CancellationToken cancellationToken)
        {
            var q = Cast(source);
            if (q.IsAlwaysFalse())
            {
                throw new InvalidOperationException("Sequence contains no elements.");
            }
            return DbAccessor.ExecuteAsync(async db =>
            {
                var query = CreateUnboundQuery(db, q.RevertOrdering());
                query.Limit(1);
                LogFirestoreQuery(q);
                var snapshot = await query.GetSnapshotAsync(cancellationToken);
                if (snapshot.Count > 0)
                {
                    return Materializer.Materialize(snapshot[0], q.Selector);
                }
                throw new InvalidOperationException("Sequence contains no elements.");
            });
        }

        protected override Task<TElement> ExecuteLastOrDefault<TElement>(IQueryable<TElement> source, CancellationToken cancellationToken)
        {
            var q = Cast(source);
            if (q.IsAlwaysFalse())
            {
                return Task.FromResult<TElement>(default!);
            }
            return DbAccessor.ExecuteAsync(async db =>
            {
                var query = CreateUnboundQuery(db, q.RevertOrdering());
                query.Limit(1);
                LogFirestoreQuery(q);
                var snapshot = await query.GetSnapshotAsync(cancellationToken);
                if (snapshot.Count > 0)
                {
                    return Materializer.Materialize(snapshot[0], q.Selector);
                }
                return default!;
            });
        }

        protected override Task<TElement> ExecuteSingle<TElement>(IQueryable<TElement> source, CancellationToken cancellationToken)
        {
            var q = Cast(source);
            if (q.IsAlwaysFalse())
            {
                throw new InvalidOperationException("Sequence contains no elements.");
            }
            return DbAccessor.ExecuteAsync(async db =>
            {
                var query = CreateUnboundQuery(db, q);
                query.Limit(2);
                LogFirestoreQuery(q);
                var snapshot = await query.GetSnapshotAsync(cancellationToken);
                return snapshot.Count switch
                {
                    0 => throw new InvalidOperationException("Sequence contains no elements."),
                    1 => Materializer.Materialize(snapshot[0], q.Selector),
                    _ => throw new InvalidOperationException("Sequence contains multiple elements."),
                };
            });
        }

        protected override Task<TElement> ExecuteSingleOrDefault<TElement>(IQueryable<TElement> source, CancellationToken cancellationToken)
        {
            var q = Cast(source);
            if (q.IsAlwaysFalse())
            {
                return Task.FromResult<TElement>(default!);
            }
            return DbAccessor.ExecuteAsync(async db =>
            {
                var query = CreateUnboundQuery(db, q);
                query.Limit(2);
                LogFirestoreQuery(q);
                var snapshot = await query.GetSnapshotAsync(cancellationToken);
                return snapshot.Count switch
                {
                    0 => default!,
                    1 => Materializer.Materialize(snapshot[0], q.Selector),
                    _ => throw new InvalidOperationException("Sequence contains multiple elements."),
                };
            });
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
                var query = CreateUnboundQuery(db, q);
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
                return Task.FromResult(Materializer.Materialize(query.StreamAsync(cancellationToken), q.Selector));
            })));
        }
    }
}