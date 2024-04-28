using System.Collections.Generic;

namespace NCoreUtils.Data.Google.Cloud.Firestore.Internal;

public interface ICollectionWrapper
{
    int Count { get; }

    void SplitIntoChunks(int size, List<object> chunks);
}
