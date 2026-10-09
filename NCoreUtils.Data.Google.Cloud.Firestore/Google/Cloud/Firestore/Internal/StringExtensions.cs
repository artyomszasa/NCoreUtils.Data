using System.Runtime.CompilerServices;

namespace NCoreUtils.Data.Google.Cloud.Firestore.Internal;

internal static class StringExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryCopyTo(this string source, Span<char> destination, out int charsWritten)
    {
        if (source.AsSpan().TryCopyTo(destination))
        {
            charsWritten = source.Length;
            return true;
        }
        charsWritten = default;
        return false;
    }
}