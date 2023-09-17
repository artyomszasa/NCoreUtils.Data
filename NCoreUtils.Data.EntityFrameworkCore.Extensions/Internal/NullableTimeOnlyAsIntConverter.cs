using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace NCoreUtils.Data.Internal;

public sealed class NullableTimeOnlyAsIntConverter : ValueConverter<TimeOnly?, int?>
{
    private static int? ToInt(TimeOnly? source)
        => source is TimeOnly t ? t.Hour * 10_000_000 + t.Minute * 100_000 + t.Second * 1000 + t.Millisecond : default(int?);

    private static TimeOnly? FromInt(int? source)
        => source is int i ? new TimeOnly(i / 10_000_000, i / 100_000 % 100, i / 1000 % 100, i % 1000) : default(TimeOnly?);

    public NullableTimeOnlyAsIntConverter()
        : base(t => ToInt(t), i => FromInt(i))
    { }
}