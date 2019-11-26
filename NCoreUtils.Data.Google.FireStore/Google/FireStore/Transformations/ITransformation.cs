using System.Collections.Generic;

namespace NCoreUtils.Data.Google.FireStore.Transformations
{
    public interface ITransformation : IValueSource
    {
        IEnumerable<IValueSource> Sources { get; }
    }

    public interface ITransformation<T> : ITransformation, IValueSource<T> { }
}