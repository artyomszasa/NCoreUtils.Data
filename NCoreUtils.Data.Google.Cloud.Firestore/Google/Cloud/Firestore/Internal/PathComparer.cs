using System;
using System.Collections.Generic;

namespace NCoreUtils.Data.Google.Cloud.Firestore.Internal
{
    /// <summary>
    /// Provides ordering comparisons for slash-separated resource paths. See <c>https://github.com/googleapis/google-cloud-dotnet/blob/13ed74b0e70f5b9932c9f679a3378493586c2fcc/apis/Google.Cloud.Firestore/Google.Cloud.Firestore/PathComparer.cs</c>.
    /// </summary>
    public sealed class PathComparer : IComparer<string>
    {
        public static PathComparer Instance { get; } = new PathComparer();

        private PathComparer()
        {
        }

        // Note: we don't handle null input, but this is never exposed, so we should never need to.
        public int Compare(string left, string right)
        {
            // We can't just do a string comparison, because of cases such as:
            // foo/bar/baz
            // foo/bar-/baz
            // Here "bar-" should come after "bar" because it's longer, but '-' is earlier than '/'.
            int length = Math.Min(left.Length, right.Length);
            for (int i = 0; i < length; i++)
            {
                char leftChar = left[i];
                char rightChar = right[i];
                if (leftChar != rightChar)
                {
                    return leftChar == '/' ? -1 // Left segment finishes earlier
                        : rightChar == '/' ? 1  // Right segment finishes earlier
                        : leftChar - rightChar; // Not a segment end, so just compare ordinals
                }
            }
            // Shorter comes before longer
            return left.Length.CompareTo(right.Length);
        }
    }
}