using Google.Apis.Auth.OAuth2;
using NCoreUtils.Data.Google.Cloud.Firestore;

namespace NCoreUtils.Data;

public class FirestoreConfiguration : IFirestoreConfiguration
{
    public string? ProjectId { get; set; }

    public FirestoreConversionOptions? ConversionOptions { get; set; }

    public GoogleCredential? GoogleCredential { get; set; }

#if NET6_0_OR_GREATER

    public System.Action<Grpc.Net.Client.GrpcChannelOptions>? ConfigureGrpcChannelOptions { get; set;}

#endif

}