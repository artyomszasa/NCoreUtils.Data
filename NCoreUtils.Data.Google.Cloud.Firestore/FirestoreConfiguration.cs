using Google.Apis.Auth.OAuth2;
using NCoreUtils.Data.Google.Cloud.Firestore;

namespace NCoreUtils.Data
{
    public class FirestoreConfiguration : IFirestoreConfiguration
    {
        public string? ProjectId { get; set; }

        public FirestoreConversionOptions? ConversionOptions { get; set; }

        public GoogleCredential? GoogleCredential { get; set; }
    }
}