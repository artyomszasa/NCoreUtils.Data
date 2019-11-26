namespace NCoreUtils.Data.Google.FireStore.Queries
{
    public static class QueryExtensions
    {
        public static IQuery<T> AddOrdering<T>(this IQuery<T> source, string path, QueryOrdering.OrderingDirection direction)
            => source.AddOrdering(new QueryOrdering(path, direction));
    }
}