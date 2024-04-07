using System;
using System.Collections.Generic;
using System.Reflection;
using NCoreUtils.Data.Model;

namespace NCoreUtils.Data.Google.Cloud.Firestore.Internal;

internal static class EnumerableOptimizations
{
    public static DataProperty FirstByProperty(this IReadOnlyList<DataProperty> source, PropertyInfo property)
    {
        if (source is null)
        {
            throw new ArgumentNullException(nameof(source));
        }
        if (property is null)
        {
            throw new ArgumentNullException(nameof(property));
        }
        foreach (var item in source)
        {
            if (item.Property == property)
            {
                return item;
            }
        }
        throw new InvalidOperationException($"DataProperty collection contains no DataProperty matching {property}.");
    }
}