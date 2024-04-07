using System;
using System.Diagnostics.CodeAnalysis;
using NCoreUtils.Data.Model;

namespace NCoreUtils.Data.Google.Cloud.Firestore.Internal;

public static class FirestoreMetadataExtensions
{
    public const string KeyFieldExpressionFactory = "Firestore:FieldExpressionFactory";

    public const string KeyEnumConversionHelpers = "Firestore:EnumConversionHelpers";

    public static bool TryGetFirestoreFieldExpressionFactory(
        this DataProperty property,
        [MaybeNullWhen(false)] out IFirestoreFieldExpressionFactory factory)
    {
        if (property.TryGetValue(KeyFieldExpressionFactory, out var boxed)
            && boxed is IFirestoreFieldExpressionFactory f)
        {
            factory = f;
            return true;
        }
        factory = default;
        return false;
    }

    public static IFirestoreFieldExpressionFactory GetFirestoreFieldExpressionFactory(this DataProperty property)
        => property.TryGetFirestoreFieldExpressionFactory(out var factory)
            ? factory
            : throw new InvalidOperationException($"No firestore field expression factory found for {property}. Either use generated model creation or reflection based decorator.");

    public static bool TryGetEnumConversionHelpers(
        this DataModel model,
        [MaybeNullWhen(false)] out IEnumConversionHelpers helpers)
    {
        if (model.TryGetValue(KeyEnumConversionHelpers, out var boxed)
            && boxed is IEnumConversionHelpers h)
        {
            helpers = h;
            return true;
        }
        helpers = default;
        return false;
    }

    public static IEnumConversionHelpers GetEnumConversionHelpers(this DataModel model)
        => model.TryGetEnumConversionHelpers(out var helper)
            ? helper
            : throw new InvalidOperationException($"No enum conversion helpers found. Either use generated model creation or reflection based decorator.");
}