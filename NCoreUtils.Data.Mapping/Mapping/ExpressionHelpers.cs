using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace NCoreUtils.Data.Mapping
{
    internal static class ExpressionHelpers
    {
        internal static ReadOnlyCollection<T> ToReadOnlyCollection<T>(this IEnumerable<T> source)
        {
            if (source is IList<T> list)
            {
                return new ReadOnlyCollection<T>(list);
            }
            return new ReadOnlyCollection<T>(source.ToList());
        }
    }
}