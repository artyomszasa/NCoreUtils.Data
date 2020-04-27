using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Google.Cloud.Firestore;
using NCoreUtils.Data.Google.Cloud.Firestore.Expressions;
using NCoreUtils.Data.Internal;

namespace NCoreUtils.Data.Google.Cloud.Firestore
{
    public partial class FirestoreQueryProvider : QueryProviderBase
    {
        protected FirestoreModel Model { get; }

        protected FirestoreDb Db { get; }

        protected FirestoreMaterializer Materializer { get; }

        public FirestoreQueryProvider(FirestoreModel model, FirestoreDb db, FirestoreMaterializer materializer)
        {
            Model = model ?? throw new ArgumentNullException(nameof(model));
            Db = db ?? throw new ArgumentNullException(nameof(db));
            Materializer = materializer ?? throw new ArgumentNullException(nameof(materializer));
        }

        protected FirestoreQuery<TElement> Cast<TElement>(IQueryable<TElement> source)
            => source as FirestoreQuery<TElement> ?? throw new InvalidOperationException($"Unable to cast {source} to firestore query.");

        protected FirestoreQuery<TElement> ApplyOrdering<TElement>(FirestoreQuery<TElement> source, LambdaExpression selector, FirestoreOrderingDirection direction)
        {
            // FIXME: propagate expression selector!!!
            var path = ResolvePath(selector.Body, selector.Parameters[0]);
            return Cast(source).AddOrdering(new FirestoreOrdering(path, direction));
        }

        /// <summary>
        /// Creates query from parameter. Only conditions are applied.
        /// </summary>
        /// <param name="source">Source query.</param>
        /// <returns>Firestore query with conditions applied</returns>
        protected Query CreateFilteredQuery(FirestoreQuery source)
        {
            Query query = Db.Collection(source.Collection);
            foreach (var condition in source.Conditions)
            {
                query = condition.Apply(query);
            }
            return query;
        }

        /// <summary>
        /// Creates query from parameter. Only conditions and ordering are applied.
        /// </summary>
        /// <param name="source">Source query.</param>
        /// <returns>Firestore query with conditions and ordering applied.</returns>
        protected Query CreateUnboundQuery(FirestoreQuery source)
        {
            var query = CreateFilteredQuery(source);
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
            throw new NotImplementedException("WIP");
        }

        protected override IOrderedQueryable<TElement> ApplyOrderBy<TElement, TKey>(IQueryable<TElement> source, Expression<Func<TElement, TKey>> selector)
            => ApplyOrdering(Cast(source), selector, FirestoreOrderingDirection.Ascending);

        protected override IOrderedQueryable<TElement> ApplyOrderByDescending<TElement, TKey>(IQueryable<TElement> source, Expression<Func<TElement, TKey>> selector)
            => ApplyOrdering(Cast(source), selector, FirestoreOrderingDirection.Descending);

        protected override IQueryable<TResult> ApplySelect<TSource, TResult>(IQueryable<TSource> source, Expression<Func<TSource, TResult>> selector)
            => Cast(source).ApplySelector(selector);

        protected override IQueryable<TResult> ApplySelect<TSource, TResult>(IQueryable<TSource> source, Expression<Func<TSource, int, TResult>> selector)
            => throw new NotImplementedException("WIP");

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
        }

        protected override IQueryable<TElement> ApplyWhere<TElement>(IQueryable<TElement> source, Expression<Func<TElement, int, bool>> predicate)
        {
        }

        protected override Task<bool> ExecuteAll<TElement>(IQueryable<TElement> source, Expression<Func<TElement, bool>> predicate, CancellationToken cancellationToken)
            => throw new NotSupportedException("Executing .All(...) would result in querying all entities.");

        protected override async Task<bool> ExecuteAny<TElement>(IQueryable<TElement> source, CancellationToken cancellationToken)
        {
            var query = CreateFilteredQuery(Cast(source));
            query.Limit(1);
            var snapshot = await query.GetSnapshotAsync(cancellationToken);
            return snapshot.Count > 0;
        }

        protected override Task<int> ExecuteCount<TElement>(IQueryable<TElement> source, CancellationToken cancellationToken)
            => throw new NotSupportedException("Executing .Count(...) would result in querying all entities.");

        protected override async Task<TElement> ExecuteFirst<TElement>(IQueryable<TElement> source, CancellationToken cancellationToken)
        {
            var q = Cast(source);
            var query = CreateUnboundQuery(q);
            query.Limit(1);
            var snapshot = await query.GetSnapshotAsync(cancellationToken);
            if (snapshot.Count > 0)
            {
                return Materializer.Materialize(snapshot[0], q.Selector);
            }
            throw new InvalidOperationException("Sequence contains no elements.");
        }

        protected override async Task<TElement> ExecuteFirstOrDefault<TElement>(IQueryable<TElement> source, CancellationToken cancellationToken)
        {
            var q = Cast(source);
            var query = CreateUnboundQuery(q);
            query.Limit(1);
            var snapshot = await query.GetSnapshotAsync(cancellationToken);
            if (snapshot.Count > 0)
            {
                return Materializer.Materialize(snapshot[0], q.Selector);
            }
            return default!;
        }

        protected override async Task<TElement> ExecuteLast<TElement>(IQueryable<TElement> source, CancellationToken cancellationToken)
        {
            var q = Cast(source);
            var query = CreateUnboundQuery(q.RevertOrdering());
            query.Limit(1);
            var snapshot = await query.GetSnapshotAsync(cancellationToken);
            if (snapshot.Count > 0)
            {
                return Materializer.Materialize(snapshot[0], q.Selector);
            }
            throw new InvalidOperationException("Sequence contains no elements.");
        }

        protected override async Task<TElement> ExecuteLastOrDefault<TElement>(IQueryable<TElement> source, CancellationToken cancellationToken)
        {
            var q = Cast(source);
            var query = CreateUnboundQuery(q.RevertOrdering());
            query.Limit(1);
            var snapshot = await query.GetSnapshotAsync(cancellationToken);
            if (snapshot.Count > 0)
            {
                return Materializer.Materialize(snapshot[0], q.Selector);
            }
            return default!;
        }

        protected override async Task<TElement> ExecuteSingle<TElement>(IQueryable<TElement> source, CancellationToken cancellationToken)
        {
            var q = Cast(source);
            var query = CreateUnboundQuery(q);
            query.Limit(2);
            var snapshot = await query.GetSnapshotAsync(cancellationToken);
            return snapshot.Count switch
            {
                0 => throw new InvalidOperationException("Sequence contains no elements."),
                1 => Materializer.Materialize(snapshot[0], q.Selector),
                _ => throw new InvalidOperationException("Sequence contains multiple elements."),
            };
        }

        protected override async Task<TElement> ExecuteSingleOrDefault<TElement>(IQueryable<TElement> source, CancellationToken cancellationToken)
        {
            var q = Cast(source);
            var query = CreateUnboundQuery(q);
            query.Limit(2);
            var snapshot = await query.GetSnapshotAsync(cancellationToken);
            return snapshot.Count switch
            {
                0 => default!,
                1 => Materializer.Materialize(snapshot[0], q.Selector),
                _ => throw new InvalidOperationException("Sequence contains multiple elements."),
            };
        }

        protected override IAsyncEnumerable<TElement> ExecuteQuery<TElement>(IQueryable<TElement> source)
        {
            var q = Cast(source);
            var query = CreateUnboundQuery(q);
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
            return new DelayedAsyncEnumerable<TElement>(
                cancellationToken => new ValueTask<IAsyncEnumerable<TElement>>(
                    Materializer.Materialize(query.StreamAsync(cancellationToken), q.Selector)
                )
            );
        }
    }
}