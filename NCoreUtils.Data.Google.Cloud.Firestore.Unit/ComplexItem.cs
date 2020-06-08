using System;
using System.Collections.Generic;

namespace NCoreUtils.Data.Google.Cloud.Firestore.Unit
{
    public class ComplexItem : IHasId<string>
    {
        public string Id { get; }

        public decimal Decimal { get; }

        public float Float { get; }

        public Guid Guid { get; }

        public SimpleItem Subitem { get; }

        public IReadOnlyList<SimpleItem> Collection { get; }

        public ComplexItem(string id, decimal @decimal, float @float, Guid guid, SimpleItem subitem, IReadOnlyList<SimpleItem> collection)
        {
            Id = id;
            Decimal = @decimal;
            Float = @float;
            Guid = guid;
            Subitem = subitem;
            Collection = collection;
        }
    }
}