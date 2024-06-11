using System.Runtime.CompilerServices;

namespace NCoreUtils.Data.Build;

public static class ConventionExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T ApplyConvention<T>(this T builder, IConvention convention)
        where T : DataPropertyBuilder
    {
        convention.Apply(builder);
        return builder;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T ApplyConvention<T>(this T builder, IConvention convention, bool applyToProperties = true)
        where T : DataEntityBuilder
    {
        convention.Apply(builder, applyToProperties);
        return builder;
    }
}