using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace NCoreUtils.Data.Internal;

public class NullableDateOnlyAsIntConverter : ValueConverter<DateOnly?, int?>
{
    public static NullableDateOnlyAsIntConverter Singleton { get; } = new();

    private static int? ToInt(DateOnly? source)
        => source is DateOnly d ? d.Year * 10_000 + d.Month * 100 + d.Day : default(int?);

    private static DateOnly? FromInt(int? source)
        => source is int i ? new DateOnly(i / 10_000, i / 100 % 100, i % 100) : default(DateOnly?);

    public NullableDateOnlyAsIntConverter()
        : base(d => ToInt(d), i => FromInt(i))
    { }
}