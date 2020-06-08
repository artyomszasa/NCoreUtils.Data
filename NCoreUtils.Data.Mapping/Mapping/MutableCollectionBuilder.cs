using System.Collections;
using System.Collections.Generic;

namespace NCoreUtils.Data.Mapping
{
    public class MutableCollectionBuilder<TCollection, TElement> : ICollectionBuilder<TElement>
        where TCollection : ICollection<TElement>, new()
    {
        private readonly TCollection _items = new TCollection();

        void ICollectionBuilder.Add(object value)
            => Add((TElement)value);

        void ICollectionBuilder.AddRange(IEnumerable values)
            => AddRange((IEnumerable<TElement>)values);

        IEnumerable ICollectionBuilder.Build() => Build();

        IEnumerable<TElement> ICollectionBuilder<TElement>.Build() => Build();

        public void Add(TElement value)
            => _items.Add(value);

        public void AddRange(IEnumerable<TElement> values)
        {
            foreach (var item in values)
            {
                Add(item);
            }
        }

        public TCollection Build()
            => _items;
    }
}