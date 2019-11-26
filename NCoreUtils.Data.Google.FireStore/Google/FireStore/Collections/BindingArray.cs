using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace NCoreUtils.Data.Google.FireStore.Collections
{
    public struct BindingArray<T> : IReadOnlyList<T>
    {
        public int ParameterBindingCount { get; }

        public ImmutableArray<T> All { get; }

        public IEnumerable<T> ParameterBindings => All.Take(ParameterBindingCount);

        public IEnumerable<T> PropertyBindings => All.Skip(ParameterBindingCount);

        public int Count => All.Length;

        public T this[int index] => All[index];

        public BindingArray(ImmutableArray<T> all, int parameterCount)
        {
            All = all;
            ParameterBindingCount = parameterCount;
        }

        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)All).GetEnumerator();

        IEnumerator<T> IEnumerable<T>.GetEnumerator() => ((IEnumerable<T>)All).GetEnumerator();

        public ImmutableArray<T>.Enumerator GetEnumerator() => All.GetEnumerator();

        public BindingArray<TResult> Map<TResult>(Func<T, TResult> selector)
        {
            var builder = ImmutableArray.CreateBuilder<TResult>(Count);
            for (var i = 0; i < Count; ++i)
            {
                builder.Add(selector(this[i]));
            }
            return new BindingArray<TResult>(builder.ToImmutable(), ParameterBindingCount);
        }
    }
}