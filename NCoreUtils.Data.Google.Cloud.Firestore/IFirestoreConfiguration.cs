using NCoreUtils.Data.Google.Cloud.Firestore;

namespace NCoreUtils.Data
{
    public interface IFirestoreConfiguration
    {
        string? ProjectId { get; }

        FirestoreConversionOptions? ConversionOptions { get; }
    }
}