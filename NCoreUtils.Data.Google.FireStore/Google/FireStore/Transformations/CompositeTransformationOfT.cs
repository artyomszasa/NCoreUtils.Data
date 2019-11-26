using System;
using System.Collections.Generic;
using System.Reflection;

namespace NCoreUtils.Data.Google.FireStore.Transformations
{
    public abstract class CompositeTransformation<T> : CompositeTransformation, ITransformation<T>
    {
        T IValueSource<T>.GetValue(object instance) => Convert(instance);

        protected override Type ResultType => typeof(T);

        public CompositeTransformation(IReadOnlyList<IValueSource> sources, IReadOnlyDictionary<PropertyInfo, IValueSource> mapping)
            : base(sources, mapping)
        { }

        public abstract T Convert(object source);

        public sealed override object DoConvert(object source) => Convert(source);
    }
}