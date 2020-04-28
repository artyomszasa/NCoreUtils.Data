using System.Collections.Immutable;
using System.Linq;
using Google.Cloud.Firestore;

namespace NCoreUtils.Data.Google.Cloud.Firestore
{
    internal static class FirestoreCollectionExtensions
    {
        internal static FieldPath ToFieldPath(this ImmutableList<string> prefix, string segment)
        {
            var array = new string[prefix.Count + 1];
            prefix.CopyTo(array);
            array[prefix.Count] = segment;
            return new FieldPath(array);
        }

        internal static FieldPath ToFieldPath(this ImmutableList<string> prefix, ImmutableList<string> rawPath)
        {
            var array = new string[prefix.Count + rawPath.Count];
            prefix.CopyTo(array);
            rawPath.CopyTo(array, prefix.Count);
            return new FieldPath(array);
        }

        internal static FieldPath ToFieldPath(this ImmutableList<string> rawPath)
            => new FieldPath(rawPath.ToArray());
    }
}