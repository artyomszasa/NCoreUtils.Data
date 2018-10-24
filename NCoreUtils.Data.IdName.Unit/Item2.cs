namespace NCoreUtils.Data
{
    public class Item2 : IHasId<int>, IHasIdName
    {
        public int Id { get; set; }

        public int ForeignId { get; set; }

        public string IdName { get; set; }

        public string Name { get; set; }
    }
}