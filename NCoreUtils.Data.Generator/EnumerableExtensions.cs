using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;

namespace NCoreUtils.Data;

internal static class GeneratorEnumerableExtensions
{
    public static bool TryGetFirst(this ImmutableArray<KeyValuePair<string, TypedConstant>> namedArguments, string name, out TypedConstant value)
    {
        foreach (var kv in namedArguments)
        {
            if (kv.Key == name)
            {
                value = kv.Value;
                return true;
            }
        }
        value = default;
        return false;
    }

    public static bool TryGetFirst<T>(this IEnumerable<T> methods, Func<T, bool> predicate, [MaybeNullWhen(false)] out T matchedMethod)
    {
        foreach (var method in methods)
        {
            if (predicate(method))
            {
                matchedMethod = method;
                return true;
            }
        }
        matchedMethod = default;
        return false;
    }

    public static IEnumerable<INamedTypeSymbol> CastAsNamedTypeSymbol(this IEnumerable<ISymbol> source)
    {
        foreach (var item in source)
        {
            if (item is INamedTypeSymbol named)
            {
                yield return named;
            }
            else
            {
                throw new InvalidCastException($"Unable to cast {item} ({item?.GetType()}) to Microsoft.CodeAnalysis.INamedTypeSymbol.");
            }
        }
    }
}