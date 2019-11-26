namespace NCoreUtils.Data.Google.FireStore.Queries
{
    public struct QueryOrdering
    {
        public enum OrderingDirection
        {
            Ascending = 0,
            Descending = 1
        }

        public string Path { get; }

        public OrderingDirection Direction { get; }

        public QueryOrdering(string path, OrderingDirection direction)
        {
            Path = path;
            Direction = direction;
        }
    }
}