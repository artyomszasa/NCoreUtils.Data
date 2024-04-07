using System;
using System.Diagnostics.CodeAnalysis;
using Google.Cloud.Firestore.V1;

namespace NCoreUtils.Data.Google.Cloud.Firestore;

public partial class FirestoreConverter
{
    protected virtual bool TryPrimitiveToValue(object? value, Type sourceType, [NotNullWhen(true)] out Value? result)
    {
        if (value is null)
        {
            result = new Value { NullValue = default };
            return true;
        }
        if (sourceType.Equals(typeof(string)))
        {
            result = FirestoreConvert.ToValue((string)value);
            return true;
        }
        if (sourceType.Equals(typeof(sbyte)))
        {
            result = FirestoreConvert.ToValue((sbyte)value);
            return true;
        }
        if (sourceType.Equals(typeof(short)))
        {
            result = FirestoreConvert.ToValue((short)value);
            return true;
        }
        if (sourceType.Equals(typeof(int)))
        {
            result = FirestoreConvert.ToValue((int)value);
            return true;
        }
        if (sourceType.Equals(typeof(long)))
        {
            result = FirestoreConvert.ToValue((long)value);
            return true;
        }
        if (sourceType.Equals(typeof(byte)))
        {
            result = FirestoreConvert.ToValue((byte)value);
            return true;
        }
        if (sourceType.Equals(typeof(ushort)))
        {
            result = FirestoreConvert.ToValue((ushort)value);
            return true;
        }
        if (sourceType.Equals(typeof(uint)))
        {
            result = FirestoreConvert.ToValue((uint)value);
            return true;
        }
        if (sourceType.Equals(typeof(ulong)))
        {
            result = FirestoreConvert.ToValue((ulong)value);
            return true;
        }
        if (sourceType.Equals(typeof(float)))
        {
            result = FirestoreConvert.ToValue((float)value);
            return true;
        }
        if (sourceType.Equals(typeof(double)))
        {
            result = FirestoreConvert.ToValue((double)value);
            return true;
        }
        if (sourceType.Equals(typeof(decimal)))
        {
            result = FirestoreConvert.ToValue((decimal)value, Options.DecimalHandling);
            return true;
        }
        if (sourceType.Equals(typeof(DateTimeOffset)))
        {
            result = FirestoreConvert.ToValue((DateTimeOffset)value);
            return true;
        }
        if (sourceType.Equals(typeof(DateTime)))
        {
            result = FirestoreConvert.ToValue((DateTime)value);
            return true;
        }
        if (sourceType.Equals(typeof(TimeSpan)))
        {
            result = FirestoreConvert.ToValue((TimeSpan)value);
            return true;
        }
        if (sourceType.Equals(typeof(bool)))
        {
            result = FirestoreConvert.ToValue((bool)value);
            return true;
        }
        if (sourceType.Equals(typeof(Guid)))
        {
            result = FirestoreConvert.ToValue((Guid)value);
            return true;
        }
        result = default;
        return false;
    }

    protected virtual bool TryPrimitiveFromValue(Value value, Type targetType, out object? result)
    {
        if (value is null)
        {
            result = new Value { NullValue = default };
            return true;
        }
        if (targetType.Equals(typeof(string)))
        {
            result = FirestoreConvert.ToString(value, Options.StrictMode);
            return true;
        }
        if (targetType.Equals(typeof(sbyte)))
        {
            result = FirestoreConvert.ToSByte(value, Options.StrictMode);
            return true;
        }
        if (targetType.Equals(typeof(short)))
        {
            result = FirestoreConvert.ToInt16(value, Options.StrictMode);
            return true;
        }
        if (targetType.Equals(typeof(int)))
        {
            result = FirestoreConvert.ToInt32(value, Options.StrictMode);
            return true;
        }
        if (targetType.Equals(typeof(long)))
        {
            result = FirestoreConvert.ToInt64(value, Options.StrictMode);
            return true;
        }
        if (targetType.Equals(typeof(byte)))
        {
            result = FirestoreConvert.ToByte(value, Options.StrictMode);
            return true;
        }
        if (targetType.Equals(typeof(ushort)))
        {
            result = FirestoreConvert.ToUInt16(value, Options.StrictMode);
            return true;
        }
        if (targetType.Equals(typeof(uint)))
        {
            result = FirestoreConvert.ToUInt32(value, Options.StrictMode);
            return true;
        }
        if (targetType.Equals(typeof(ulong)))
        {
            result = FirestoreConvert.ToUInt64(value, Options.StrictMode);
            return true;
        }
        if (targetType.Equals(typeof(float)))
        {
            result = FirestoreConvert.ToSingle(value, Options.StrictMode);
            return true;
        }
        if (targetType.Equals(typeof(double)))
        {
            result = FirestoreConvert.ToDouble(value, Options.StrictMode);
            return true;
        }
        if (targetType.Equals(typeof(decimal)))
        {
            result = FirestoreConvert.ToDecimal(value, Options.StrictMode);
            return true;
        }
        if (targetType.Equals(typeof(DateTimeOffset)))
        {
            result = FirestoreConvert.ToDateTimeOffset(value, Options.StrictMode);
            return true;
        }
        if (targetType.Equals(typeof(DateTime)))
        {
            result = FirestoreConvert.ToDateTime(value, Options.StrictMode);
            return true;
        }
        if (targetType.Equals(typeof(TimeSpan)))
        {
            result = FirestoreConvert.ToTimeSpan(value, Options.StrictMode);
            return true;
        }
        if (targetType.Equals(typeof(bool)))
        {
            result = FirestoreConvert.ToBoolean(value, Options.StrictMode);
            return true;
        }
        if (targetType.Equals(typeof(Guid)))
        {
            result = FirestoreConvert.ToGuid(value, Options.StrictMode);
            return true;
        }
        result = default;
        return false;
    }
}