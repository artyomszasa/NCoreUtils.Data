using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Linq.Expressions;
using Google.Cloud.Firestore;
using NCoreUtils.Data.Mapping;

namespace NCoreUtils.Data.Google.Cloud.Firestore
{
    public abstract class FirestoreQuery : IOrderedQueryable
    {
        #region IQueryable

        IQueryProvider IQueryable.Provider => Provider;

        public abstract Type ElementType { get; }

        public Expression Expression => Expression.Constant(this);

        public FirestoreQueryProvider Provider { get; }

        #endregion

        #region Firestore

        public string Collection { get; }

        public abstract LambdaExpression SelectorExpression { get; }

        public ImmutableList<FirestoreCondition> Conditions { get; }

        public ImmutableList<FirestoreOrdering> Ordering { get; }

        public int Offset { get; }

        public int? Limit { get; }

        #endregion

        public FirestoreQuery(
            FirestoreQueryProvider provider,
            string collection,
            ImmutableList<FirestoreCondition> conditions,
            ImmutableList<FirestoreOrdering> ordering,
            int offset,
            int? limit)
        {
            Provider = provider;
            Collection = collection;
            Conditions = conditions;
            Ordering = ordering;
            Offset = offset;
            Limit = limit;
        }

        IEnumerator IEnumerable.GetEnumerator() => GetBoxedEnumerator();

        protected abstract IEnumerator GetBoxedEnumerator();

        public abstract FirestoreQuery ReplaceConditions(ImmutableList<FirestoreCondition> conditions);
    }

    public class FirestoreQuery<T> : FirestoreQuery, IOrderedQueryable<T>
    {
        public override Type ElementType => typeof(T);

        public override LambdaExpression SelectorExpression => Selector;

        public Expression<Func<DocumentSnapshot, T>> Selector { get; }

        public FirestoreQuery(
            FirestoreQueryProvider provider,
            string collection,
            Expression<Func<DocumentSnapshot, T>> selector,
            ImmutableList<FirestoreCondition> conditions,
            ImmutableList<FirestoreOrdering> ordering,
            int offset,
            int? limit)
            : base(provider, collection, conditions, ordering, offset, limit)
            => Selector = selector ?? throw new ArgumentNullException(nameof(selector));

        protected override IEnumerator GetBoxedEnumerator()
            => GetEnumerator();

        public IEnumerator<T> GetEnumerator()
            => Provider.ExecuteEnumerableAsync<T>(Expression).ToEnumerable().GetEnumerator();

        public FirestoreQuery<TResult> ApplySelector<TResult>(Expression<Func<T, TResult>> selector)
            => new FirestoreQuery<TResult>(
                Provider,
                Collection,
                Selector.ChainSimplified(selector, true),
                Conditions,
                Ordering,
                Offset,
                Limit
            );

        public FirestoreQuery<T> AddCondition(in FirestoreCondition condition)
            => new FirestoreQuery<T>(
                Provider,
                Collection,
                Selector,
                Conditions.Add(condition),
                Ordering,
                Offset,
                Limit
            );

        public FirestoreQuery<T> AddConditions(IEnumerable<FirestoreCondition> conditions)
            => new FirestoreQuery<T>(
                Provider,
                Collection,
                Selector,
                Conditions.AddRange(conditions),
                Ordering,
                Offset,
                Limit
            );

        public override FirestoreQuery ReplaceConditions(ImmutableList<FirestoreCondition> conditions)
            => new FirestoreQuery<T>(
                Provider,
                Collection,
                Selector,
                conditions,
                Ordering,
                Offset,
                Limit
            );

        public FirestoreQuery<T> AddOrdering(in FirestoreOrdering ordering)
            => new FirestoreQuery<T>(
                Provider,
                Collection,
                Selector,
                Conditions,
                Ordering.Add(ordering),
                Offset,
                Limit
            );

        public FirestoreQuery<T> ReverseOrder()
            => new FirestoreQuery<T>(
                Provider,
                Collection,
                Selector,
                Conditions,
                // TODO: optimize
                Ordering
                    .Select(o => new FirestoreOrdering(o.Path, o.Direction == FirestoreOrderingDirection.Ascending ? FirestoreOrderingDirection.Descending : FirestoreOrderingDirection.Ascending))
                    .ToImmutableList(),
                Offset,
                Limit
            );

        public FirestoreQuery<T> ApplyOffset(int offset)
            => new FirestoreQuery<T>(
                Provider,
                Collection,
                Selector,
                Conditions,
                Ordering,
                offset,
                Limit
            );

        public FirestoreQuery<T> ApplyLimit(int limit)
            => new FirestoreQuery<T>(
                Provider,
                Collection,
                Selector,
                Conditions,
                Ordering,
                Offset,
                limit
            );

        public FirestoreQuery<T> RevertOrdering()
            => new FirestoreQuery<T>(
                Provider,
                Collection,
                Selector,
                Conditions,
                // FIXME: optimize
                Ordering.Select(o => o.Revert()).ToImmutableList(),
                Offset,
                Limit
            );
    }
}