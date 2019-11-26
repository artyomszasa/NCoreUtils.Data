using System;

namespace NCoreUtils.Data.Google.FireStore.Transformations
{
    public interface IValueSource
    {
        Type Type { get; }
        object GetValue(object instance);
    }
}