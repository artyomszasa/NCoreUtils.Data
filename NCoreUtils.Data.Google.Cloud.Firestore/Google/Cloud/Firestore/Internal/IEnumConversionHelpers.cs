using System;
using System.Diagnostics.CodeAnalysis;

namespace NCoreUtils.Data.Google.Cloud.Firestore.Internal;

public interface IEnumConversionHelpers
{
    bool TryGetHelper(Type enumType, [MaybeNullWhen(false)] out IEnumConversionHelper helper);

    IEnumConversionHelper GetHelper(Type enumType)
        => TryGetHelper(enumType, out var helper)
            ? helper
            : throw new InvalidOperationException($"No enum conversion helper has been registered for {enumType}.");
}