using Google.Cloud.Firestore.V1;

namespace NCoreUtils.Data.Google.Cloud.Firestore.Internal;

public abstract class EnumConversionHelper<T> : IEnumConversionHelper
    where T : struct, System.Enum
{
    public abstract IEnumInfo<T> Info { get; }

    public Value Convert(object enumValue, FirestoreEnumHandling enumHandling)
        => FirestoreConvert.ToValue((T)enumValue, Info, enumHandling);

    public string ToFlagsString(object enumValue)
        => FirestoreConvert.EnumToFlagsString((T)enumValue, Info);
}