using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace NCoreUtils.Data.Internal;

public sealed class DateTimeOffsetAsLongConverter : ValueConverter<DateTimeOffset, long>
{
    public static DateTimeOffsetAsLongConverter Singleton { get; } = new();

    public DateTimeOffsetAsLongConverter()
        : base(d => d.UtcTicks, utcTicks => new DateTimeOffset(utcTicks, TimeSpan.Zero))
    { }
}