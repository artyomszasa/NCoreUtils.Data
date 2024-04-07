using System.Collections.Immutable;
using Google.Cloud.Firestore;

namespace NCoreUtils.Data.Google.Cloud.Firestore;

internal static class FirestoreCollectionExtensions
{
    /// <summary>
    /// Creates <see cref="FieldPath" /> by concatenating supplied parameters.
    /// <para>
    /// Note that expressions have the following syntax: outer-most-property(outer-property(field-expression)) thus
    /// the <paramref name="propertyPath" /> is passed in the reverse order i.e. [outer-most-property, outer-property].
    /// This order is reversed in the path created, i.e.
    /// [...field-expression.path, outer-property, outer-most-property].
    /// </para>
    /// </summary>
    /// <param name="propertyPath">
    /// Outer property names used to access the field defined by the <paramref name="rawPath" />
    /// </param>
    /// <param name="rawPath">
    /// Raw path of the target field.
    /// </param>
    internal static FieldPath ToFieldPath(this ImmutableList<string> propertyPath, ImmutableList<string> rawPath)
    {
        var len = propertyPath.Count + rawPath.Count;
        var array = new string[len];
        // place rawPath of the direct field
        rawPath.CopyTo(array);
        // place propertyPath in the reversed order
        var lastIndex = len - 1;
        for (var sourceIndex = 0; sourceIndex < propertyPath.Count; ++sourceIndex)
        {
            var targetIndex = lastIndex - sourceIndex;
            array[targetIndex] = propertyPath[sourceIndex];
        }
        // create field path
        return new FieldPath(array);
    }
}