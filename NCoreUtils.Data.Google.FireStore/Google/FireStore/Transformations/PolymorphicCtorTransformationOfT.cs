using System;
using System.Collections.Generic;
using System.Reflection;

namespace NCoreUtils.Data.Google.FireStore.Transformations
{
    public class PolymorphicCtorTransformation<T> : PolymorphicCtorTransformation, ITransformation<T>
    {
        public override Type Type => typeof(T);

        public PolymorphicCtorTransformation(IValueSource<string> typeExtractor, IReadOnlyDictionary<string, ITransformation> derivates)
            : base(typeExtractor, derivates)
        { }

        public new T GetValue(object instance) => (T)base.GetValue(instance);
    }
}