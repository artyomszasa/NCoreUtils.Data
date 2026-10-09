namespace NCoreUtils.Data.Google.Cloud.Firestore.Internal;

public interface ICollectionWrapper
{
    int Count { get; }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1002:Do not expose generic lists", Justification = "List provides better optimization here.")]
    void SplitIntoChunks(int size, List<object> chunks);
}
