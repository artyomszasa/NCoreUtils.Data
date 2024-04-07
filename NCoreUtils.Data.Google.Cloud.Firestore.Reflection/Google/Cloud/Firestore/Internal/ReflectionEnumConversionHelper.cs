namespace NCoreUtils.Data.Google.Cloud.Firestore.Internal;

public sealed class ReflectionEnumConversionHelper<T> : EnumConversionHelper<T>
    where T : struct, Enum
{
    public override IEnumInfo<T> Info { get; } = new ReflectionEnumInfo<T>();
}