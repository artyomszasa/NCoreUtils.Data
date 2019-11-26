using System;
using System.Collections;
using System.Linq;
using System.Linq.Expressions;

namespace NCoreUtils.Data.Google.FireStore.Queries
{
    public abstract class Query : IQuery
    {
        readonly TinyList<QueryOrdering> _ordering;

        readonly TinyList<Condition> _conditions;

        IQueryProvider IQueryable.Provider => Provider;

        public ref readonly TinyList<Condition> Conditions => ref _conditions;

        public abstract Type ElementType { get; }

        public virtual Expression Expression => Expression.Constant(this);

        public int Limit { get; }

        public QueryProvider Provider { get; }

        public int Offset { get; }

        public ref readonly TinyList<QueryOrdering> Ordering => ref _ordering;

        public Query(QueryProvider provider, in TinyList<Condition> conditions, in TinyList<QueryOrdering> ordering, int offset, int limit)
        {
            Provider = provider ?? throw new ArgumentNullException(nameof(provider));
            _conditions = conditions;
            _ordering = ordering;
            Offset = offset;
            Limit = limit;
        }

        IEnumerator IEnumerable.GetEnumerator() => GetBoxedEnumerator();

        public abstract IEnumerator GetBoxedEnumerator();
    }
}