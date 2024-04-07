using System;
using System.Collections.Generic;

namespace NCoreUtils.Data.Google.Cloud.Firestore;

public class FirestoreConversionOptions(
    bool strictMode,
    FirestoreDecimalHandling decimalHandling,
    FirestoreEnumHandling enumHandling,
    IReadOnlyList<FirestoreValueConverter> converters)
{
    public static FirestoreConversionOptions Default { get; } = new FirestoreConversionOptions(
        true,
        FirestoreDecimalHandling.AsString,
        FirestoreEnumHandling.AlwaysAsString,
        []
    );

    public bool StrictMode { get; } = strictMode;

    public FirestoreDecimalHandling DecimalHandling { get; } = decimalHandling;

    public FirestoreEnumHandling EnumHandling { get; } = enumHandling;

    public IReadOnlyList<FirestoreValueConverter> Converters { get; } = converters ?? throw new ArgumentNullException(nameof(converters));
}