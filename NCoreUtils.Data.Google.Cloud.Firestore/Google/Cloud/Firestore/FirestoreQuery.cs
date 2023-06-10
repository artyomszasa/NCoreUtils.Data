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

        public ImmutableHashSet<FirestoreCondition> Conditions { get; }

        public ImmutableList<FirestoreOrdering> Ordering { get; }

        public ImmutableHashSet<FieldPath> ShadowFields { get; }

        public int Offset { get; }

        public int? Limit { get; }

        #endregion

        public FirestoreQuery(
            FirestoreQueryProvider provider,
            string collection,
            ImmutableHashSet<FirestoreCondition> conditions,
            ImmutableList<FirestoreOrdering> ordering,
            ImmutableHashSet<FieldPath> shadowFields,
            int offset,
            int? limit)
        {
            Provider = provider;
            Collection = collection;
            Conditions = conditions;
            Ordering = ordering;
            ShadowFields = shadowFields;
            Offset = offset;
            Limit = limit;
        }

        IEnumerator IEnumerable.GetEnumerator() => GetBoxedEnumerator();

        protected abstract IEnumerator GetBoxedEnumerator();

        public abstract FirestoreQuery ReplaceConditions(ImmutableHashSet<FirestoreCondition> conditions);

        public abstract FirestoreQuery AddShadowField(FieldPath path);
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
            ImmutableHashSet<FirestoreCondition> conditions,
            ImmutableList<FirestoreOrdering> ordering,
            ImmutableHashSet<FieldPath> shadowFields,
            int offset,
            int? limit)
            : base(provider, collection, conditions, ordering, shadowFields, offset, limit)
            => Selector = selector ?? throw new ArgumentNullException(nameof(selector));

        protected override IEnumerator GetBoxedEnumerator()
            => GetEnumerator();

        public IEnumerator<T> GetEnumerator()
            => Provider.ExecuteEnumerableAsync<T>(Expression).ToEnumerable().GetEnumerator();

        public FirestoreQuery<TResult> ApplySelector<TResult>(Expression<Func<T, TResult>> selector)
            => new(
                Provider,
                Collection,
                Selector.ChainSimplified(selector, true),
                Conditions,
                Ordering,
                ShadowFields,
                Offset,
                Limit
            );

        public FirestoreQuery<T> AddCondition(in FirestoreCondition condition)
            => new(
                Provider,
                Collection,
                Selector,
                Conditions.Add(condition),
                Ordering,
                ShadowFields,
                Offset,
                Limit
            );

        public FirestoreQuery<T> AddConditions(IEnumerable<FirestoreCondition> conditions)
            => new(
                Provider,
                Collection,
                Selector,
                conditions.Aggregate(Conditions, (set, condition) => set.Add(condition)),
                Ordering,
                ShadowFields,
                Offset,
                Limit
            );

        public override FirestoreQuery ReplaceConditions(ImmutableHashSet<FirestoreCondition> conditions)
            => new FirestoreQuery<T>(
                Provider,
                Collection,
                Selector,
                conditions,
                Ordering,
                ShadowFields,
                Offset,
                Limit
            );

        public FirestoreQuery<T> AddOrdering(in FirestoreOrdering ordering)
            => new(
                Provider,
                Collection,
                Selector,
                Conditions,
                Ordering.Add(ordering),
                ShadowFields,
                Offset,
                Limit
            );

        public FirestoreQuery<T> ReverseOrder()
            => new(
                Provider,
                Collection,
                Selector,
                Conditions,
                // TODO: optimize
                Ordering
                    .Select(o => new FirestoreOrdering(o.Path, o.Direction == FirestoreOrderingDirection.Ascending ? FirestoreOrderingDirection.Descending : FirestoreOrderingDirection.Ascending))
                    .ToImmutableList(),
                ShadowFields,
                Offset,
                Limit
            );

        public FirestoreQuery<T> ApplyOffset(int offset)
            => new(
                Provider,
                Collection,
                Selector,
                Conditions,
                Ordering,
                ShadowFields,
                offset,
                Limit
            );

        public FirestoreQuery<T> ApplyLimit(int limit)
            => new(
                Provider,
                Collection,
                Selector,
                Conditions,
                Ordering,
                ShadowFields,
                Offset,
                limit
            );

        public FirestoreQuery<T> RevertOrdering()
            => new(
                Provider,
                Collection,
                Selector,
                Conditions,
                // TODO: optimize
                Ordering.Select(o => o.Revert()).ToImmutableList(),
                ShadowFields,
                Offset,
                Limit
            );

        public override FirestoreQuery AddShadowField(FieldPath path)
            => new FirestoreQuery<T>(
                Provider,
                Collection,
                Selector,
                Conditions,
                Ordering,
                ShadowFields.Add(path),
                Offset,
                Limit
            );
    }
}