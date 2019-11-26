using System.Collections.Generic;
using Google.Cloud.Firestore;

namespace NCoreUtils.Data.Google.FireStore.Queries
{
    public interface IDocumentConverter<T>
    {
        IEnumerable<string> GetUsedFields();

        T Convert(DocumentSnapshot docRef);
    }
}