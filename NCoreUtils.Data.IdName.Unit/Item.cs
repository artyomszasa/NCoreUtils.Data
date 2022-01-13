namespace NCoreUtils.Data
{
    public class Item : IHasId<int>, IHasIdName
    {
        public int Id { get; set; }

        public string IdName { get; set; } = default!;

        public string Name { get; set; } = default!;
    }
}