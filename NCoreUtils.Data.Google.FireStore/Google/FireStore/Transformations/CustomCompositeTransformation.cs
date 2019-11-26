using System;
using System.Collections.Generic;

namespace NCoreUtils.Data.Google.FireStore.Transformations
{
    public static class CustomCompositeTransformation
    {
        public static CompositeTransformation FromDelegate(IReadOnlyList<IValueSource> sources, Delegate @delegate)
        {
            var resultType = @delegate.Method.ReturnType;
            return (CompositeTransformation)Activator.CreateInstance(
                typeof(CustomCompositeTransformation<>).MakeGenericType(resultType),
                new object[] { @delegate }
            );
        }
    }
}