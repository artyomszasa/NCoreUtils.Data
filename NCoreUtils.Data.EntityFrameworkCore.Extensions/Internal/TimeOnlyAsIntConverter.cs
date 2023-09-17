using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace NCoreUtils.Data.Internal;

public class TimeOnlyAsIntConverter : ValueConverter<TimeOnly, int>
{
    public static TimeOnlyAsIntConverter Singleton { get; } = new();

    public TimeOnlyAsIntConverter()
        : base(
            t => t.Hour * 10_000_000 + t.Minute * 100_000 + t.Second * 1000 + t.Millisecond,
            i => new TimeOnly(i / 10_000_000, i / 100_000 % 100, i / 1000 % 100, i % 1000)
        )
    { }
}