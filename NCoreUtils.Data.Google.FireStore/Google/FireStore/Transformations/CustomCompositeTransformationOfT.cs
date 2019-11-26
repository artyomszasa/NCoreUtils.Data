using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reflection;

namespace NCoreUtils.Data.Google.FireStore.Transformations
{
    public class CustomCompositeTransformation<T> : CompositeTransformation<T>
    {
        public Func<object, T> Converter { get; }

        public CustomCompositeTransformation(IReadOnlyList<IValueSource> sources, Func<object, T> converter)
            : base(sources, ImmutableDictionary<PropertyInfo, IValueSource>.Empty)
        {
            Converter = converter ?? throw new ArgumentNullException(nameof(converter));
        }

        public override T Convert(object source) => Converter(source);
    }
}