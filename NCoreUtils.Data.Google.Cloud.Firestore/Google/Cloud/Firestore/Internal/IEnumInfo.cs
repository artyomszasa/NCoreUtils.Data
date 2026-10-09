namespace NCoreUtils.Data.Google.Cloud.Firestore.Internal;

public interface IEnumInfo<T>
    where T : struct, Enum
{
    IReadOnlyList<T> GetValues();
}