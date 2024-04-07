
namespace NCoreUtils.Data.Google.Cloud.Firestore.Internal;

internal sealed class ReflectionEnumInfo<T> : IEnumInfo<T>
    where T : struct, Enum
{
    private IReadOnlyList<T>? Values { get; set; }

    public IReadOnlyList<T> GetValues()
        => Values ??= Enum.GetValues(typeof(T)).Cast<T>().ToArray();
}