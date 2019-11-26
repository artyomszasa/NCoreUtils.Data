using System.Linq;

namespace NCoreUtils.Data.Google.FireStore.Queries
{
    public interface IQuery : IOrderedQueryable
    {
        ref readonly TinyList<QueryOrdering> Ordering { get; }

        ref readonly TinyList<Condition> Conditions { get; }

        int Limit { get; }

        int Offset { get; }
    }
}