using System;
using System.Collections.Generic;

namespace NCoreUtils.Data.Google.Cloud.Firestore.Unit
{
    public class NestedSubitem
    {
        public SimpleItem Data { get; }

        public NestedSubitem(SimpleItem data)
        {
            Data = data;
        }
    }

    public class ComplexItem : IHasId<string>
    {
        public string Id { get; }

        public int? Nint1 { get; }

        public int? Nint2 { get; }

        public decimal Decimal { get; }

        public float Float { get; }

        public Guid Guid { get; }

        public SimpleItem Subitem { get; }

        public IReadOnlyList<SimpleItem> Collection { get; }

        public HashSet<SimpleItem> Set { get; }

        public NestedSubitem Nested { get; }

        public ComplexItem(
            string id,
            int? nint1,
            int? nint2,
            decimal @decimal,
            float @float,
            Guid guid,
            SimpleItem subitem,
            IReadOnlyList<SimpleItem> collection,
            HashSet<SimpleItem> set,
            NestedSubitem nested)
        {
            Id = id;
            Nint1 = nint1;
            Nint2 = nint2;
            Decimal = @decimal;
            Float = @float;
            Guid = guid;
            Subitem = subitem;
            Collection = collection;
            Set = set;
            Nested = nested;
        }
    }
}