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
                DecimalHandling = source.DecimalHandling,
                EnumHandling = source.EnumHandling
            };
            builder.Converters.AddRange(source.Converters);
            return builder;
        }

        public bool StrictMode { get; set; } = true;

        public FirestoreDecimalHandling DecimalHandling { get; set; }

        public FirestoreEnumHandling EnumHandling { get; set; }

        public List<FirestoreValueConverter> Converters { get; } = new List<FirestoreValueConverter>();

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
            foreach (var converter in converters)
            {
                Converters.Add(converter);
            }
            return this;
        }

        public FirestoreConversionOptions ToOptions()
            => new FirestoreConversionOptions(StrictMode, DecimalHandling, EnumHandling, Converters.ToArray());
    }
}