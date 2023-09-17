using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace NCoreUtils.Data.Internal;

public sealed class NullableDateTimeOffsetAsLongConverter : ValueConverter<DateTimeOffset?, long?>
{
    public static NullableDateTimeOffsetAsLongConverter Singleton { get; } = new();

    private static long? ToUtcTicks(DateTimeOffset? source)
        => source is DateTimeOffset dt ? dt.UtcTicks : default(long?);

    private static DateTimeOffset? FromUtcTicks(long? source)
        => source is long utcTicks ? new DateTimeOffset(utcTicks, TimeSpan.Zero) : default(DateTimeOffset?);

    public NullableDateTimeOffsetAsLongConverter()
        : base(d => ToUtcTicks(d), utcTicks => FromUtcTicks(utcTicks))
    { }
}