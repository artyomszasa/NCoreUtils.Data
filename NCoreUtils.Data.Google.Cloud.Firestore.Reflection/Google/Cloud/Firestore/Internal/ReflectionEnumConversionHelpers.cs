#if NET8_0_OR_GREATER
using System.Collections.Frozen;
#endif
using System.Diagnostics.CodeAnalysis;

namespace NCoreUtils.Data.Google.Cloud.Firestore.Internal;

public sealed class ReflectionEnumConversionHelpers(IEnumerable<Type> enumTypes) : IEnumConversionHelpers
{
#if NET8_0_OR_GREATER
    private FrozenDictionary<Type, IEnumConversionHelper> Helpers { get; } = enumTypes.ToFrozenDictionary(
        keySelector: static type => type,
        elementSelector: static type => (IEnumConversionHelper)Activator.CreateInstance(
            typeof(ReflectionEnumConversionHelper<>).MakeGenericType(type)
        )!
    );
#else
    private IReadOnlyDictionary<Type, IEnumConversionHelper> Helpers { get; } = enumTypes.ToDictionary(
        keySelector: static type => type,
        elementSelector: static type => (IEnumConversionHelper)Activator.CreateInstance(
            typeof(ReflectionEnumConversionHelper<>).MakeGenericType(type)
        )!
    );
#endif


    public bool TryGetHelper(Type enumType, [MaybeNullWhen(false)] out IEnumConversionHelper helper)
        => Helpers.TryGetValue(enumType, out helper);
}