using NCoreUtils.Data.Google.Cloud.Firestore;

namespace NCoreUtils.Data
{
    public class FirestoreConfiguration : IFirestoreConfiguration
    {
        public string? ProjectId { get; set; }

        public FirestoreConversionOptions? ConversionOptions { get; set; }
    }
}