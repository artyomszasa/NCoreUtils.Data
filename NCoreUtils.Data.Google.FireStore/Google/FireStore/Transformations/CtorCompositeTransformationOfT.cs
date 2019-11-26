using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reflection;
using NCoreUtils.Data.Google.FireStore.Collections;

namespace NCoreUtils.Data.Google.FireStore.Transformations
{
    public class CtorCompositeTransformation<T> : CompositeTransformation<T>
    {
        public ConstructorInfo Ctor { get; }

        public BindingArray<PropertyInfo> Bindings { get; }

        public CtorCompositeTransformation(IReadOnlyList<IValueSource> sources, IReadOnlyDictionary<PropertyInfo, IValueSource> mapping, ConstructorInfo ctor, BindingArray<PropertyInfo> bindings)
            : base(sources, mapping)
        {
            Ctor = ctor ?? throw new ArgumentNullException(nameof(ctor));
            Bindings = bindings;
        }

        public override T Convert(object source)
        {
            var sources = PopulateSources(source);
            var result = (T)Ctor.Invoke(sources.Slice(0, Bindings.ParameterBindingCount));
            for (var i = Bindings.ParameterBindingCount; i < Bindings.Count; ++i)
            {
                Bindings[i].SetValue(result, sources[i], null);
            }
            return result;
        }
    }
}