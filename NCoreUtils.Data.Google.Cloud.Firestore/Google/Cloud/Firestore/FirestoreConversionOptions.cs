using System;
using System.Collections.Generic;

namespace NCoreUtils.Data.Google.Cloud.Firestore
{
    public class FirestoreConversionOptions
    {
        public static FirestoreConversionOptions Default { get; } = new FirestoreConversionOptions(
            true,
            FirestoreDecimalHandling.AsString,
            FirestoreEnumHandling.AlwaysAsString,
            Array.Empty<FirestoreValueConverter>()
        );

        public bool StrictMode { get; }

        public FirestoreDecimalHandling DecimalHandling { get; }

        public FirestoreEnumHandling EnumHandling { get; }

        public IReadOnlyList<FirestoreValueConverter> Converters { get; }

        public FirestoreConversionOptions(
            bool strictMode,
            FirestoreDecimalHandling decimalHandling,
            FirestoreEnumHandling enumHandling,
            IReadOnlyList<FirestoreValueConverter> converters)
        {
            StrictMode = strictMode;
            DecimalHandling = decimalHandling;
            EnumHandling = enumHandling;
            Converters = converters ?? throw new ArgumentNullException(nameof(converters));
        }
    }
}