using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace NCoreUtils.Data.Google.Cloud.Firestore.Internal;

internal static class FlowHelpers
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [return: NotNullIfNotNull(nameof(variable))]
    public static T? Exchange<T>(
        ref T? variable,
        [NotNullIfNotNull(nameof(variable))] T? value)
        where T : class
    {
        return Interlocked.Exchange(ref variable, value);
        // var current = variable;
        // variable = value;
        // return current;
    }
}