using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using Google.Cloud.Firestore;
using NCoreUtils.Data.Google.FireStore.Transformations;

namespace NCoreUtils.Data.Google.FireStore.Queries
{
    public class DirectQuery<T> : Query, IQuery<T>
    {
        public override Type ElementType => typeof(T);

        public ITransformation<T> Transformation { get; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [DebuggerStepThrough]
        public DirectQuery(
            QueryProvider provider,
            ITransformation<T> transformation,
            in TinyList<Condition> conditions,
            in TinyList<QueryOrdering> ordering,
            int offset,
            int limit)
            : base(provider, in conditions, in ordering, offset, limit)
        {
            Transformation = transformation ?? throw new ArgumentNullException(nameof(transformation));
        }

        IEnumerable<string> IDocumentConverter<T>.GetUsedFields()
            => Transformation.Sources
                .OfType<DocumentPropertySource>()
                .Select(source => source.Path)
                .Distinct();

        #region IQuery

        IQuery<T> IQuery<T>.AddConditions(in TinyList<Condition> conditions) => AddConditions(in conditions);

        IQuery<T> IQuery<T>.AddOrdering(QueryOrdering ordering) => AddOrdering(ordering);

        IQuery<T> IQuery<T>.WithLimit(int limit) => WithLimit(limit);

        IQuery<T> IQuery<T>.WithOffset(int offset) => WithOffset(offset);

        #endregion

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public DirectQuery<T> AddConditions(in TinyList<Condition> conditions)
            => new DirectQuery<T>(
                Provider,
                Transformation,
                Conditions.CopyAdd(conditions),
                in Ordering,
                Offset,
                Limit);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public DirectQuery<T> AddOrdering(QueryOrdering ordering)
            => new DirectQuery<T>(
                Provider,
                Transformation,
                in Conditions,
                Ordering.CopyAdd(ordering),
                Offset,
                Limit);

        public T Convert(DocumentSnapshot document) => Transformation.GetValue(document);

        public IEnumerator<T> GetEnumerator()
            => Provider.ExecuteQuery(this, CancellationToken.None).ToEnumerable().GetEnumerator();

        public override IEnumerator GetBoxedEnumerator() => GetEnumerator();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public DirectQuery<T> WithLimit(int limit)
            => new DirectQuery<T>(
                Provider,
                Transformation,
                in Conditions,
                in Ordering,
                Offset,
                limit);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public DirectQuery<T> WithOffset(int offset)
            => new DirectQuery<T>(
                Provider,
                Transformation,
                in Conditions,
                in Ordering,
                offset,
                Limit);

    }
}