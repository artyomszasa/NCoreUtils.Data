using System.Collections.Generic;

namespace NCoreUtils.Data.Google.Cloud.Firestore.Internal;

public interface IEnumInfo<T>
    where T : struct, System.Enum
{
    IReadOnlyList<T> GetValues();
}