using System.Collections.Generic;

namespace NCoreUtils.Data.Google.Cloud.Firestore;

public class FirestoreConversionOptionsBuilder
{
    public static FirestoreConversionOptionsBuilder FromOptions(FirestoreConversionOptions source)
    {
        Preconditions.ThrowIfNull(source);
        var builder = new FirestoreConversionOptionsBuilder
        {
            StrictMode = source.StrictMode,
            DecimalHandling = source.DecimalHandling,
            EnumHandling = source.EnumHandling
        };
        builder.Converters.AddRange(source.Converters);
        return builder;
    }

    public bool StrictMode { get; set; } = true;

    public FirestoreDecimalHandling DecimalHandling { get; set; }

    public FirestoreEnumHandling EnumHandling { get; set; }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1002:Do not expose generic lists", Justification = "List is intended here.")]
    public List<FirestoreValueConverter> Converters { get; } = [];

    public FirestoreConversionOptionsBuilder SetStrictMode(bool strictMode)
    {
        StrictMode = strictMode;
        return this;
    }

    public FirestoreConversionOptionsBuilder SetDecimalHandling(FirestoreDecimalHandling decimalHandling)
    {
        DecimalHandling = decimalHandling;
        return this;
    }

    public FirestoreConversionOptionsBuilder SetEnumHandling(FirestoreEnumHandling enumHandling)
    {
        EnumHandling = enumHandling;
        return this;
    }

    public FirestoreConversionOptionsBuilder AddConverter(FirestoreValueConverter converter)
    {
        Converters.Add(converter);
        return this;
    }

    public FirestoreConversionOptionsBuilder AddConverters(params FirestoreValueConverter[] converters)
    {
        Preconditions.ThrowIfNull(converters);
        foreach (var converter in converters)
        {
            Converters.Add(converter);
        }
        return this;
    }

    public FirestoreConversionOptions ToOptions()
        => new(StrictMode, DecimalHandling, EnumHandling, Converters.ToArray());
}