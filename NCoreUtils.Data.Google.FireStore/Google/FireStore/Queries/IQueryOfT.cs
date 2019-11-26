using System.Linq;

namespace NCoreUtils.Data.Google.FireStore.Queries
{
    public interface IQuery<T> : IQuery, IOrderedQueryable<T>, IDocumentConverter<T>
    {
        IQuery<T> AddOrdering(QueryOrdering ordering);

        IQuery<T> AddConditions(in TinyList<Condition> conditions);

        IQuery<T> WithLimit(int limit);

        IQuery<T> WithOffset(int offset);
    }
}