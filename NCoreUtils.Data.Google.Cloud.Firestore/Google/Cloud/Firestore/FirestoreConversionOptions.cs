using System;
using System.Collections.Generic;

namespace NCoreUtils.Data.Google.Cloud.Firestore
{
    public class FirestoreConversionOptions
    {
        public static FirestoreConversionOptions Default { get; } = new FirestoreConversionOptions(
            true,
            FirestoreDecimalHandling.AsString,
            new FirestoreValueConverter[0]
        );

        public bool StrictMode { get; }

        public FirestoreDecimalHandling DecimalHandling { get; }

        public IReadOnlyList<FirestoreValueConverter> Converters { get; }

        public FirestoreConversionOptions(
            bool strictMode,
            FirestoreDecimalHandling decimalHandling,
            IReadOnlyList<FirestoreValueConverter> converters)
        {
            StrictMode = strictMode;
            DecimalHandling = decimalHandling;
            Converters = converters ?? throw new ArgumentNullException(nameof(converters));
        }
    }
}