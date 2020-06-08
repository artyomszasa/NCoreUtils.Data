using System.Collections;
using System.Collections.Generic;

namespace NCoreUtils.Data
{
    public interface ICollectionBuilder
    {
        void Add(object value);

        void AddRange(IEnumerable values);

        IEnumerable Build();
    }

    public interface ICollectionBuilder<T> : ICollectionBuilder
    {
        void Add(T value);

        void AddRange(IEnumerable<T> values);

        new IEnumerable<T> Build();
    }
}