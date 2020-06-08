using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using Google.Cloud.Firestore.V1;
using Google.Protobuf;
#if NETSTANDARD2_0
using Convert = NCoreUtils.PolyfillConvert;
using BitConverter = NCoreUtils.PolyfillBitConverter;
#endif

namespace NCoreUtils.Data.Google.Cloud.Firestore
{
    public static class FirestoreConvert
    {
        private static readonly string[] _truthy = new string[]
        {
            "true",
            "on",
            "1"
        };

        internal static IReadOnlyList<string> ThruthyValues => _truthy;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static Guid GuidFromByteString(ByteString value)
        {
            #if NETSTANDARD2_0
            var buffer = new byte[value.Length];
            value.CopyTo(buffer, 0);
            return new Guid(buffer);
            #else
            return new Guid(value.Span);
            #endif
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static string ToBase64(ByteString bytes)
        {
            var bufferSize = (bytes.Length + 3 - 1) / 3 * 4;
            if (bufferSize <= 8192)
            {
                Span<char> buffer = stackalloc char[bufferSize];
                if (Convert.TryToBase64Chars(bytes.Span, buffer, out var written, Base64FormattingOptions.None))
                {
                    return buffer.Slice(0, written).ToString();
                }
            }
            // fallback to heap memory
            var managedBuffer = new byte[bytes.Length];
            bytes.CopyTo(managedBuffer, 0);
            return Convert.ToBase64String(managedBuffer);
        }

        public static string ToString(Value value, bool strict)
        {
            if (strict)
            {
                return value.ValueTypeCase switch
                {
                    Value.ValueTypeOneofCase.StringValue => value.StringValue,
                    Value.ValueTypeOneofCase.ReferenceValue => value.ReferenceValue,
                    var @case => throw new FirestoreConversionException(typeof(string), @case)
                };
            }
            return value.ValueTypeCase switch
            {
                Value.ValueTypeOneofCase.NullValue => string.Empty,
                Value.ValueTypeOneofCase.StringValue => value.StringValue,
                Value.ValueTypeOneofCase.BooleanValue => value.BooleanValue.ToString(CultureInfo.InvariantCulture),
                Value.ValueTypeOneofCase.BytesValue => ToBase64(value.BytesValue),
                Value.ValueTypeOneofCase.DoubleValue => value.DoubleValue.ToString(CultureInfo.InvariantCulture),
                Value.ValueTypeOneofCase.IntegerValue => value.IntegerValue.ToString(CultureInfo.InvariantCulture),
                Value.ValueTypeOneofCase.ReferenceValue => value.ReferenceValue,
                Value.ValueTypeOneofCase.TimestampValue => value.TimestampValue.ToDateTimeOffset().ToString("o", CultureInfo.InvariantCulture),
                var @case => throw new FirestoreConversionException(typeof(string), @case)
            };
        }

        public static sbyte ToSByte(Value value, bool strict)
        {
            if (strict)
            {
                if (Value.ValueTypeOneofCase.IntegerValue == value.ValueTypeCase)
                {
                    try
                    {
                        return Convert.ToSByte(value.IntegerValue);
                    }
                    catch (Exception exn)
                    {
                        throw new FirestoreConversionException(typeof(sbyte), value.ValueTypeCase, exn);
                    }
                }
                throw new FirestoreConversionException(typeof(sbyte), value.ValueTypeCase);
            }
            switch (value.ValueTypeCase)
            {
                case Value.ValueTypeOneofCase.NullValue:
                    return default;
                case Value.ValueTypeOneofCase.BooleanValue:
                    return Convert.ToSByte(value.BooleanValue);
                case Value.ValueTypeOneofCase.DoubleValue:
                    return unchecked((sbyte)value.DoubleValue);
                case Value.ValueTypeOneofCase.IntegerValue:
                    return unchecked((sbyte)value.IntegerValue);
                case Value.ValueTypeOneofCase.StringValue:
                    try
                    {
                        return sbyte.Parse(value.StringValue, NumberStyles.Integer, CultureInfo.InvariantCulture);
                    }
                    catch (Exception exn)
                    {
                        throw new FirestoreConversionException(typeof(sbyte), value.ValueTypeCase, exn);
                    }
                default:
                    throw new FirestoreConversionException(typeof(sbyte), value.ValueTypeCase);
            }
        }

        public static short ToInt16(Value value, bool strict)
        {
            if (strict)
            {
                if (Value.ValueTypeOneofCase.IntegerValue == value.ValueTypeCase)
                {
                    try
                    {
                        return Convert.ToInt16(value.IntegerValue);
                    }
                    catch (Exception exn)
                    {
                        throw new FirestoreConversionException(typeof(short), value.ValueTypeCase, exn);
                    }
                }
                throw new FirestoreConversionException(typeof(short), value.ValueTypeCase);
            }
            switch (value.ValueTypeCase)
            {
                case Value.ValueTypeOneofCase.NullValue:
                    return default;
                case Value.ValueTypeOneofCase.BooleanValue:
                    return Convert.ToInt16(value.BooleanValue);
                case Value.ValueTypeOneofCase.DoubleValue:
                    return unchecked((short)value.DoubleValue);
                case Value.ValueTypeOneofCase.IntegerValue:
                    return unchecked((short)value.IntegerValue);
                case Value.ValueTypeOneofCase.StringValue:
                    try
                    {
                        return short.Parse(value.StringValue, NumberStyles.Integer, CultureInfo.InvariantCulture);
                    }
                    catch (Exception exn)
                    {
                        throw new FirestoreConversionException(typeof(short), value.ValueTypeCase, exn);
                    }
                default:
                    throw new FirestoreConversionException(typeof(short), value.ValueTypeCase);
            }
        }

        public static int ToInt32(Value value, bool strict)
        {
            if (strict)
            {
                if (Value.ValueTypeOneofCase.IntegerValue == value.ValueTypeCase)
                {
                    try
                    {
                        return Convert.ToInt32(value.IntegerValue);
                    }
                    catch (Exception exn)
                    {
                        throw new FirestoreConversionException(typeof(int), value.ValueTypeCase, exn);
                    }
                }
                throw new FirestoreConversionException(typeof(int), value.ValueTypeCase);
            }
            switch (value.ValueTypeCase)
            {
                case Value.ValueTypeOneofCase.NullValue:
                    return default;
                case Value.ValueTypeOneofCase.BooleanValue:
                    return Convert.ToInt32(value.BooleanValue);
                case Value.ValueTypeOneofCase.DoubleValue:
                    return unchecked((int)value.DoubleValue);
                case Value.ValueTypeOneofCase.IntegerValue:
                    return unchecked((int)value.IntegerValue);
                case Value.ValueTypeOneofCase.StringValue:
                    try
                    {
                        return int.Parse(value.StringValue, NumberStyles.Integer, CultureInfo.InvariantCulture);
                    }
                    catch (Exception exn)
                    {
                        throw new FirestoreConversionException(typeof(int), value.ValueTypeCase, exn);
                    }
                default:
                    throw new FirestoreConversionException(typeof(int), value.ValueTypeCase);
            }
        }

        public static long ToInt64(Value value, bool strict)
        {
            if (strict)
            {
                if (Value.ValueTypeOneofCase.IntegerValue == value.ValueTypeCase)
                {
                    return value.IntegerValue;
                }
                throw new FirestoreConversionException(typeof(long), value.ValueTypeCase);
            }
            switch (value.ValueTypeCase)
            {
                case Value.ValueTypeOneofCase.NullValue:
                    return default;
                case Value.ValueTypeOneofCase.BooleanValue:
                    return Convert.ToInt64(value.BooleanValue);
                case Value.ValueTypeOneofCase.DoubleValue:
                    return unchecked((long)value.DoubleValue);
                case Value.ValueTypeOneofCase.IntegerValue:
                    return value.IntegerValue;
                case Value.ValueTypeOneofCase.StringValue:
                    try
                    {
                        return long.Parse(value.StringValue, NumberStyles.Integer, CultureInfo.InvariantCulture);
                    }
                    catch (Exception exn)
                    {
                        throw new FirestoreConversionException(typeof(long), value.ValueTypeCase, exn);
                    }
                case Value.ValueTypeOneofCase.TimestampValue:
                    return value.TimestampValue.ToDateTimeOffset().UtcTicks;
                default:
                    throw new FirestoreConversionException(typeof(long), value.ValueTypeCase);
            }
        }

        public static byte ToByte(Value value, bool strict)
        {
            if (strict)
            {
                if (Value.ValueTypeOneofCase.IntegerValue == value.ValueTypeCase)
                {
                    try
                    {
                        return Convert.ToByte(value.IntegerValue);
                    }
                    catch (Exception exn)
                    {
                        throw new FirestoreConversionException(typeof(byte), value.ValueTypeCase, exn);
                    }
                }
                throw new FirestoreConversionException(typeof(byte), value.ValueTypeCase);
            }
            switch (value.ValueTypeCase)
            {
                case Value.ValueTypeOneofCase.NullValue:
                    return default;
                case Value.ValueTypeOneofCase.BooleanValue:
                    return Convert.ToByte(value.BooleanValue);
                case Value.ValueTypeOneofCase.DoubleValue:
                    return unchecked((byte)value.DoubleValue);
                case Value.ValueTypeOneofCase.IntegerValue:
                    return unchecked((byte)value.IntegerValue);
                case Value.ValueTypeOneofCase.StringValue:
                    try
                    {
                        return byte.Parse(value.StringValue, NumberStyles.Integer, CultureInfo.InvariantCulture);
                    }
                    catch (Exception exn)
                    {
                        throw new FirestoreConversionException(typeof(byte), value.ValueTypeCase, exn);
                    }
                default:
                    throw new FirestoreConversionException(typeof(byte), value.ValueTypeCase);
            }
        }

        public static ushort ToUInt16(Value value, bool strict)
        {
            if (strict)
            {
                if (Value.ValueTypeOneofCase.IntegerValue == value.ValueTypeCase)
                {
                    try
                    {
                        return Convert.ToUInt16(value.IntegerValue);
                    }
                    catch (Exception exn)
                    {
                        throw new FirestoreConversionException(typeof(ushort), value.ValueTypeCase, exn);
                    }
                }
                throw new FirestoreConversionException(typeof(ushort), value.ValueTypeCase);
            }
            switch (value.ValueTypeCase)
            {
                case Value.ValueTypeOneofCase.NullValue:
                    return default;
                case Value.ValueTypeOneofCase.BooleanValue:
                    return Convert.ToUInt16(value.BooleanValue);
                case Value.ValueTypeOneofCase.DoubleValue:
                    return unchecked((ushort)value.DoubleValue);
                case Value.ValueTypeOneofCase.IntegerValue:
                    return unchecked((ushort)value.IntegerValue);
                case Value.ValueTypeOneofCase.StringValue:
                    try
                    {
                        return ushort.Parse(value.StringValue, NumberStyles.Integer, CultureInfo.InvariantCulture);
                    }
                    catch (Exception exn)
                    {
                        throw new FirestoreConversionException(typeof(ushort), value.ValueTypeCase, exn);
                    }
                default:
                    throw new FirestoreConversionException(typeof(ushort), value.ValueTypeCase);
            }
        }

        public static uint ToUInt32(Value value, bool strict)
        {
            if (strict)
            {
                if (Value.ValueTypeOneofCase.IntegerValue == value.ValueTypeCase)
                {
                    try
                    {
                        return Convert.ToUInt32(value.IntegerValue);
                    }
                    catch (Exception exn)
                    {
                        throw new FirestoreConversionException(typeof(int), value.ValueTypeCase, exn);
                    }
                }
                throw new FirestoreConversionException(typeof(int), value.ValueTypeCase);
            }
            switch (value.ValueTypeCase)
            {
                case Value.ValueTypeOneofCase.NullValue:
                    return default;
                case Value.ValueTypeOneofCase.BooleanValue:
                    return Convert.ToUInt32(value.BooleanValue);
                case Value.ValueTypeOneofCase.DoubleValue:
                    return unchecked((uint)value.DoubleValue);
                case Value.ValueTypeOneofCase.IntegerValue:
                    return unchecked((uint)value.IntegerValue);
                case Value.ValueTypeOneofCase.StringValue:
                    try
                    {
                        return uint.Parse(value.StringValue, NumberStyles.Integer, CultureInfo.InvariantCulture);
                    }
                    catch (Exception exn)
                    {
                        throw new FirestoreConversionException(typeof(uint), value.ValueTypeCase, exn);
                    }
                default:
                    throw new FirestoreConversionException(typeof(uint), value.ValueTypeCase);
            }
        }

        public static ulong ToUInt64(Value value, bool strict)
        {
            if (strict)
            {
                if (Value.ValueTypeOneofCase.IntegerValue == value.ValueTypeCase)
                {
                    try
                    {
                        return Convert.ToUInt64(value.IntegerValue);
                    }
                    catch (Exception exn)
                    {
                        throw new FirestoreConversionException(typeof(ulong), value.ValueTypeCase, exn);
                    }
                }
                throw new FirestoreConversionException(typeof(ulong), value.ValueTypeCase);
            }
            switch (value.ValueTypeCase)
            {
                case Value.ValueTypeOneofCase.NullValue:
                    return default;
                case Value.ValueTypeOneofCase.BooleanValue:
                    return Convert.ToUInt64(value.BooleanValue);
                case Value.ValueTypeOneofCase.DoubleValue:
                    return unchecked((ulong)value.DoubleValue);
                case Value.ValueTypeOneofCase.IntegerValue:
                    return unchecked((ulong)value.IntegerValue);
                case Value.ValueTypeOneofCase.StringValue:
                    try
                    {
                        return ulong.Parse(value.StringValue, NumberStyles.Integer, CultureInfo.InvariantCulture);
                    }
                    catch (Exception exn)
                    {
                        throw new FirestoreConversionException(typeof(ulong), value.ValueTypeCase, exn);
                    }
                default:
                    throw new FirestoreConversionException(typeof(ulong), value.ValueTypeCase);
            }
        }

        public static float ToSingle(Value value, bool strict)
        {
            if (strict)
            {
                if (Value.ValueTypeOneofCase.DoubleValue == value.ValueTypeCase)
                {
                    try
                    {
                        return Convert.ToSingle(value.DoubleValue);
                    }
                    catch (Exception exn)
                    {
                        throw new FirestoreConversionException(typeof(float), value.ValueTypeCase, exn);
                    }
                }
                throw new FirestoreConversionException(typeof(float), value.ValueTypeCase);
            }
            switch (value.ValueTypeCase)
            {
                case Value.ValueTypeOneofCase.NullValue:
                    return default;
                case Value.ValueTypeOneofCase.BooleanValue:
                    return Convert.ToSingle(value.BooleanValue);
                case Value.ValueTypeOneofCase.DoubleValue:
                    return unchecked((float)value.DoubleValue);
                case Value.ValueTypeOneofCase.IntegerValue:
                    return value.IntegerValue;
                case Value.ValueTypeOneofCase.StringValue:
                    try
                    {
                        return float.Parse(value.StringValue, NumberStyles.Float, CultureInfo.InvariantCulture);
                    }
                    catch (Exception exn)
                    {
                        throw new FirestoreConversionException(typeof(float), value.ValueTypeCase, exn);
                    }
                default:
                    throw new FirestoreConversionException(typeof(float), value.ValueTypeCase);
            }
        }

        public static double ToDouble(Value value, bool strict)
        {
            if (strict)
            {
                if (Value.ValueTypeOneofCase.DoubleValue == value.ValueTypeCase)
                {
                    return value.DoubleValue;
                }
                throw new FirestoreConversionException(typeof(double), value.ValueTypeCase);
            }
            switch (value.ValueTypeCase)
            {
                case Value.ValueTypeOneofCase.NullValue:
                    return default;
                case Value.ValueTypeOneofCase.BooleanValue:
                    return Convert.ToDouble(value.BooleanValue);
                case Value.ValueTypeOneofCase.DoubleValue:
                    return value.DoubleValue;
                case Value.ValueTypeOneofCase.IntegerValue:
                    return value.IntegerValue;
                case Value.ValueTypeOneofCase.StringValue:
                    try
                    {
                        return double.Parse(value.StringValue, NumberStyles.Float, CultureInfo.InvariantCulture);
                    }
                    catch (Exception exn)
                    {
                        throw new FirestoreConversionException(typeof(double), value.ValueTypeCase, exn);
                    }
                default:
                    throw new FirestoreConversionException(typeof(double), value.ValueTypeCase);
            }
        }

        /// <summary>
        /// Converts value to <see cref="decimal" />.
        /// <para>
        /// Due to firestore having no native support for decimal values all of the following
        /// representations are allowed in strict mode: <c>bytes</c>, <c>string</c>, <c>integer</c> and <c>double</c>.
        /// </para>
        /// </summary>
        /// <param name="value">Value to convert.</param>
        /// <param name="strict">Whether to use strict mode.</param>
        /// <returns>Converted value.</returns>
        public static decimal ToDecimal(Value value, bool strict)
        {
            if (strict)
            {
                return value.ValueTypeCase switch
                {
                    Value.ValueTypeOneofCase.BytesValue => FromBytes(value.BytesValue),
                    Value.ValueTypeOneofCase.StringValue => FromString(value.StringValue),
                    Value.ValueTypeOneofCase.IntegerValue => new decimal(value.IntegerValue),
                    Value.ValueTypeOneofCase.DoubleValue => new decimal(value.DoubleValue),
                    var @case => throw new FirestoreConversionException(typeof(decimal), @case)
                };
            }
            return value.ValueTypeCase switch
            {
                Value.ValueTypeOneofCase.BytesValue => FromBytes(value.BytesValue),
                Value.ValueTypeOneofCase.StringValue => FromString(value.StringValue),
                Value.ValueTypeOneofCase.IntegerValue => new decimal(value.IntegerValue),
                Value.ValueTypeOneofCase.DoubleValue => new decimal(value.DoubleValue),
                Value.ValueTypeOneofCase.BooleanValue => value.BooleanValue ? decimal.One : decimal.Zero,
                Value.ValueTypeOneofCase.NullValue => decimal.Zero,
                var @case => throw new FirestoreConversionException(typeof(decimal), @case)
            };

            static decimal FromString(string value)
            {
                try
                {
                    return decimal.Parse(value, NumberStyles.Float, CultureInfo.InvariantCulture);
                }
                catch (Exception exn)
                {
                    throw new FirestoreConversionException(typeof(decimal), Value.ValueTypeOneofCase.StringValue, exn);
                }
            }

            static decimal FromBytes(ByteString bytes)
            {
                try
                {
                    var ints = new int[4];
                    ReadOnlySpan<byte> source = bytes.Span;
                    ints[0] = BitConverter.ToInt32(source.Slice(0, 4));
                    ints[1] = BitConverter.ToInt32(source.Slice(4, 4));
                    ints[2] = BitConverter.ToInt32(source.Slice(8, 4));
                    ints[3] = BitConverter.ToInt32(source.Slice(12, 4));
                    return new decimal(ints);
                }
                catch (Exception exn)
                {
                    throw new FirestoreConversionException(typeof(decimal), Value.ValueTypeOneofCase.BytesValue, exn);
                }
            }
        }

        public static DateTimeOffset ToDateTimeOffset(Value value, bool strict)
        {
            if (strict)
            {
                return value.ValueTypeCase switch
                {
                    Value.ValueTypeOneofCase.TimestampValue => value.TimestampValue.ToDateTimeOffset(),
                    var @case => throw new FirestoreConversionException(typeof(DateTimeOffset), @case)
                };
            }
            switch (value.ValueTypeCase)
            {
                case Value.ValueTypeOneofCase.TimestampValue:
                    return value.TimestampValue.ToDateTimeOffset();
                case Value.ValueTypeOneofCase.IntegerValue:
                    return new DateTimeOffset(value.IntegerValue, TimeSpan.Zero);
                case Value.ValueTypeOneofCase.StringValue:
                    try
                    {
                        return DateTimeOffset.Parse(value.StringValue, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.AssumeUniversal);
                    }
                    catch (Exception exn)
                    {
                        throw new FirestoreConversionException(typeof(DateTimeOffset), value.ValueTypeCase, exn);
                    }
                default:
                    throw new FirestoreConversionException(typeof(DateTimeOffset), value.ValueTypeCase);
            }
        }

        public static DateTime ToDateTime(Value value, bool strict)
        {
            if (strict)
            {
                return value.ValueTypeCase switch
                {
                    Value.ValueTypeOneofCase.TimestampValue => value.TimestampValue.ToDateTime(),
                    var @case => throw new FirestoreConversionException(typeof(DateTime), @case)
                };
            }
            switch (value.ValueTypeCase)
            {
                case Value.ValueTypeOneofCase.TimestampValue:
                    return value.TimestampValue.ToDateTime();
                case Value.ValueTypeOneofCase.IntegerValue:
                    return new DateTimeOffset(value.IntegerValue, TimeSpan.Zero).DateTime;
                case Value.ValueTypeOneofCase.StringValue:
                    try
                    {
                        return DateTime.Parse(value.StringValue, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.AssumeUniversal);
                    }
                    catch (Exception exn)
                    {
                        throw new FirestoreConversionException(typeof(DateTime), value.ValueTypeCase, exn);
                    }
                default:
                    throw new FirestoreConversionException(typeof(DateTime), value.ValueTypeCase);
            }
        }

        /// <summary>
        /// Converts value to <see cref="decimal" />.
        /// <para>
        /// Due to firestore having no native support for time span values we assume that the time span is stored as
        /// total number of ticks in strict mode.
        /// </para>
        /// </summary>
        /// <param name="value">Value to convert.</param>
        /// <param name="strict">Whether to use strict mode.</param>
        /// <returns>Converted value.</returns>
        public static TimeSpan ToTimeSpan(Value value, bool strict)
        {
            if (strict)
            {
                return value.ValueTypeCase switch
                {
                    Value.ValueTypeOneofCase.IntegerValue => new TimeSpan(value.IntegerValue),
                    var @case => throw new FirestoreConversionException(typeof(TimeSpan), @case)
                };
            }
            switch (value.ValueTypeCase)
            {
                case Value.ValueTypeOneofCase.IntegerValue:
                    return new TimeSpan(value.IntegerValue);
                case Value.ValueTypeOneofCase.NullValue:
                    return TimeSpan.Zero;
                case Value.ValueTypeOneofCase.StringValue:
                    try
                    {
                        return TimeSpan.Parse(value.StringValue, CultureInfo.InvariantCulture);
                    }
                    catch (Exception exn)
                    {
                        throw new FirestoreConversionException(typeof(TimeSpan), value.ValueTypeCase, exn);
                    }
                default:
                    throw new FirestoreConversionException(typeof(TimeSpan), value.ValueTypeCase);
            }
        }

        public static bool ToBoolean(Value value, bool strict)
        {
            if (strict)
            {
                return value.ValueTypeCase switch
                {
                    Value.ValueTypeOneofCase.BooleanValue => value.BooleanValue,
                    var @case => throw new FirestoreConversionException(typeof(bool), @case)
                };
            }
            return value.ValueTypeCase switch
            {
                Value.ValueTypeOneofCase.BooleanValue => value.BooleanValue,
                Value.ValueTypeOneofCase.DoubleValue => value.DoubleValue != 0.0,
                Value.ValueTypeOneofCase.IntegerValue => value.IntegerValue != 0,
                Value.ValueTypeOneofCase.StringValue => Array.Exists(_truthy, v => StringComparer.InvariantCultureIgnoreCase.Equals(v, value.StringValue)),
                Value.ValueTypeOneofCase.NullValue => false,
                var @case => throw new FirestoreConversionException(typeof(bool), @case)
            };
        }

        public static Guid ToGuid(Value value, bool strict)
        {
            if (strict)
            {
                if (value.ValueTypeCase == Value.ValueTypeOneofCase.BytesValue)
                {
                    try
                    {
                        return GuidFromByteString(value.BytesValue);
                    }
                    catch (Exception exn)
                    {
                        throw new FirestoreConversionException(typeof(Guid), Value.ValueTypeOneofCase.BytesValue, exn);
                    }
                }
                throw new FirestoreConversionException(typeof(Guid), value.ValueTypeCase);
            }
            try
            {
                return value.ValueTypeCase switch
                {
                    Value.ValueTypeOneofCase.BytesValue => GuidFromByteString(value.BytesValue),
                    Value.ValueTypeOneofCase.StringValue => new Guid(Convert.FromBase64String(value.StringValue)),
                    var @case => throw new FirestoreConversionException(typeof(Guid), @case)
                };
            }
            catch (Exception exn) when (!(exn is FirestoreConversionException))
            {
                throw new FirestoreConversionException(typeof(Guid), value.ValueTypeCase, exn);
            }
        }

        public static Value ToValue(string value)
            => new Value { StringValue = value };

        public static Value ToValue(sbyte value)
            => new Value { IntegerValue = value };

        public static Value ToValue(short value)
            => new Value { IntegerValue = value };

        public static Value ToValue(int value)
            => new Value { IntegerValue = value };

        public static Value ToValue(long value)
            => new Value { IntegerValue = value };

        public static Value ToValue(byte value)
            => new Value { IntegerValue = value };

        public static Value ToValue(ushort value)
            => new Value { IntegerValue = value };

        public static Value ToValue(uint value)
            => new Value { IntegerValue = value };

        public static Value ToValue(ulong value)
            => new Value { IntegerValue = Convert.ToInt64(value) };

        public static Value ToValue(float value)
            => new Value { DoubleValue = value };

        public static Value ToValue(double value)
            => new Value { DoubleValue = value };

        public static Value ToValue(decimal value, FirestoreDecimalHandling decimalHandling)
        {
            return decimalHandling switch
            {
                FirestoreDecimalHandling.AsBinary => ToBinaryValue(value),
                FirestoreDecimalHandling.AsDouble => new Value { DoubleValue = decimal.ToDouble(value) },
                FirestoreDecimalHandling.AsString => new Value { StringValue = value.ToString(CultureInfo.InvariantCulture) },
                FirestoreDecimalHandling.RoundToInteger => new Value { IntegerValue = decimal.ToInt64(decimal.Round(value)) },
                _ => throw new InvalidOperationException("Invalid decimal handling option.")
            };

            static Value ToBinaryValue(decimal value)
            {
                var ints = decimal.GetBits(value);
                Span<byte> bytes = stackalloc byte[16];
                BitConverter.TryWriteBytes(bytes.Slice(0, 4), ints[0]);
                BitConverter.TryWriteBytes(bytes.Slice(4, 4), ints[1]);
                BitConverter.TryWriteBytes(bytes.Slice(8, 4), ints[2]);
                BitConverter.TryWriteBytes(bytes.Slice(12, 4), ints[3]);
                return new Value { BytesValue = ByteString.CopyFrom(bytes) };
            }
        }

        public static Value ToValue(DateTimeOffset value)
            => new Value { TimestampValue = global::Google.Protobuf.WellKnownTypes.Timestamp.FromDateTimeOffset(value) };

        public static Value ToValue(DateTime value)
            => new Value { TimestampValue = global::Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(value) };

        public static Value ToValue(TimeSpan value)
            => new Value { IntegerValue = value.Ticks };

        public static Value ToValue(bool value)
            => new Value { BooleanValue = value };

        public static Value ToValue(Guid value)
            => new Value { BytesValue = ByteString.CopyFrom(value.ToByteArray()) };
    }
}