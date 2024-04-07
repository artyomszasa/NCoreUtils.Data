using Google.Cloud.Firestore.V1;

namespace NCoreUtils.Data.Google.Cloud.Firestore.Internal;

public interface IEnumConversionHelper
{
    Value Convert(object enumValue, FirestoreEnumHandling enumHandling);

    string ToFlagsString(object enumValue);
}