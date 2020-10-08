using System;
using System.Collections.Generic;

namespace NCoreUtils.Data.InMemory
{
    internal static class ListExtensions
    {
        public static int FindIndex<T>(this IList<T> list, Func<T, bool> predicate)
        {
            if (list is null)
            {
                throw new ArgumentNullException(nameof(list));
            }
            if (predicate is null)
            {
                throw new ArgumentNullException(nameof(predicate));
            }
            for (var i = 0; i < list.Count; ++i)
            {
                if (predicate(list[i]))
                {
                    return i;
                }
            }
            return -1;
        }
    }
}