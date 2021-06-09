using System.Collections.Generic;

namespace NCoreUtils.Data.Google.Cloud.Firestore.Unit
{
    public class ItemWithArray : IHasId<string>
    {
        public string Id { get; }

        public string Name { get; }

        public IReadOnlyList<string> Values { get; }

        public ItemWithArray(string id, string name, IReadOnlyList<string> values)
        {
            Id = id;
            Name = name;
            Values = values;
        }
    }
}