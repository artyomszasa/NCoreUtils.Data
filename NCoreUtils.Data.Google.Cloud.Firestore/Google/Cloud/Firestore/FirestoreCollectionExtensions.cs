using System.Collections.Immutable;
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
    }
}