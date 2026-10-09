using System;
using System.Diagnostics.CodeAnalysis;
using Google.Cloud.Firestore.V1;
using NCoreUtils.Data.Google.Cloud.Firestore.Internal;

namespace NCoreUtils.Data.Google.Cloud.Firestore
{
    public partial class FirestoreConverter
    {
        protected virtual bool TryEnumToValue(object? value, Type sourceType, [NotNullWhen(true)] out Value? result)
        {
            Preconditions.ThrowIfNull(sourceType);
            if (sourceType.IsEnum)
            {
                var helper = Model.GetEnumConversionHelpers().GetHelper(sourceType);
                result = helper.Convert(value!, Options.EnumHandling);
                return true;
            }
            result = default;
            return false;
        }

        protected virtual bool TryEnumFromValue(Value value, Type targetType, out object? result)
        {
            Preconditions.ThrowIfNull(targetType);
            if (targetType.IsEnum)
            {
                result = FirestoreConvert.ToEnum(targetType, value, Options.StrictMode);
                return true;
            }
            result = default;
            return false;
        }
    }
}