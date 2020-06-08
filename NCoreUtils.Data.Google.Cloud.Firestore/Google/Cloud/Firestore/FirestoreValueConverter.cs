using System;
using Google.Cloud.Firestore.V1;

namespace NCoreUtils.Data.Google.Cloud.Firestore
{
    public abstract class FirestoreValueConverter
    {
        internal abstract object? ConvertFromValue(Value value, Type targetType, FirestoreConverter converter);

        internal abstract Value ConvertToValue(object? value, Type sourceType, FirestoreConverter converter);

        public abstract bool CanConvert(Type type);
    }

    public abstract class FirestoreValueConverter<T> : FirestoreValueConverter
    {
        protected abstract T FromValue(Value value, Type targetType, FirestoreConverter converter);

        protected abstract Value ToValue(T value, Type sourceType, FirestoreConverter converter);

        internal sealed override object? ConvertFromValue(Value value, Type targetType, FirestoreConverter converter)
            => FromValue(value, targetType, converter);

        internal sealed override Value ConvertToValue(object? value, Type sourceType, FirestoreConverter converter)
            => ToValue((T)value!, sourceType, converter);

        public override bool CanConvert(Type type) => type.Equals(typeof(T));
    }
}