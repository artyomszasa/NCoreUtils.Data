using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace NCoreUtils.Data.Internal;

public class DateOnlyAsIntConverter : ValueConverter<DateOnly, int>
{
    public static DateOnlyAsIntConverter Singleton { get; } = new();

    public DateOnlyAsIntConverter()
        : base(
            d => d.Year * 10_000 + d.Month * 100 + d.Day,
            i => new DateOnly(i / 10_000, i / 100 % 100, i % 100)
        )
    { }
}