using System.Collections.Generic;

namespace NCoreUtils.Data.Google.Cloud.Firestore
{
    public class FirestoreConversionOptionsBuilder
    {
        public static FirestoreConversionOptionsBuilder FromOptions(FirestoreConversionOptions source)
        {
            var builder = new FirestoreConversionOptionsBuilder
            {
                StrictMode = source.StrictMode,
                DecimalHandling = source.DecimalHandling
            };
            builder.Converters.AddRange(source.Converters);
            return builder;
        }

        public bool StrictMode { get; set; } = true;

        public FirestoreDecimalHandling DecimalHandling { get; set; }

        public List<FirestoreValueConverter> Converters { get; } = new List<FirestoreValueConverter>();

        public FirestoreConversionOptions ToOptions()
            => new FirestoreConversionOptions(StrictMode, DecimalHandling, Converters.ToArray());
    }
}