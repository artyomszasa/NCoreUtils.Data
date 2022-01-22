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
            for (var i = 0; i <= prefix.Count; ++i)
            {
                if (0 == i)
                {
                    array[i] = segment;
                }
                else
                {
                    array[i] = prefix[prefix.Count - i - 1];
                }
            }
            return new FieldPath(array);
        }

        // FIXME: optimize
        internal static FieldPath ToFieldPath(this ImmutableList<string> prefix, ImmutableList<string> rawPath)
        {
            // var array = new string[prefix.Count + rawPath.Count];
            // prefix.CopyTo(array);
            // rawPath.CopyTo(array, prefix.Count);
            // return new FieldPath(array);
            return new FieldPath(rawPath.Concat(prefix.Reverse()).ToArray());
        }

        internal static FieldPath ToFieldPath(this ImmutableList<string> rawPath)
            => new(rawPath.ToArray());
    }
}