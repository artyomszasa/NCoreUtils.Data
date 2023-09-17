using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NCoreUtils.Data.Internal;

namespace NCoreUtils.Data;

public static class ModelBuilderExtensions
{
    public static PropertyBuilder<DateTimeOffset> HasUtcTicksConversion(this PropertyBuilder<DateTimeOffset> builder)
        => builder
            .HasConversion(DateTimeOffsetAsLongConverter.Singleton)
            .IsRequired(true);

    public static PropertyBuilder<DateTimeOffset?> HasUtcTicksConversion(this PropertyBuilder<DateTimeOffset?> builder)
        => builder
            .HasConversion(NullableDateTimeOffsetAsLongConverter.Singleton)
            .IsRequired(false);

    public static PropertyBuilder<DateOnly> HasIntConversion(this PropertyBuilder<DateOnly> builder)
        => builder
            .HasConversion(DateOnlyAsIntConverter.Singleton)
            .IsRequired(true);

    public static PropertyBuilder<DateOnly?> HasIntConversion(this PropertyBuilder<DateOnly?> builder)
        => builder
            .HasConversion(NullableDateOnlyAsIntConverter.Singleton)
            .IsRequired(true);
}