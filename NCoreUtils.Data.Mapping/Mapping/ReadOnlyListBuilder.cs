using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace NCoreUtils.Data.Mapping
{
    public class ReadOnlyListBuilder<T> : ICollectionBuilder<T>
    {
        private readonly List<T> _items = new();

        void ICollectionBuilder.Add(object value)
            => Add((T)value);

        void ICollectionBuilder.AddRange(IEnumerable values)
            => AddRange(values.Cast<T>());

        IEnumerable ICollectionBuilder.Build() => Build();

        IEnumerable<T> ICollectionBuilder<T>.Build() => Build();

        public void Add(T value)
            => _items.Add(value);

        public void AddRange(IEnumerable<T> values)
            => _items.AddRange(values);

        public IReadOnlyList<T> Build()
            => _items.ToArray();
    }
}