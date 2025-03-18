using Google.Apis.Auth.OAuth2;
using NCoreUtils.Data.Google.Cloud.Firestore;

namespace NCoreUtils.Data;

public interface IFirestoreConfiguration
{
    string? ProjectId { get; }

    FirestoreConversionOptions? ConversionOptions { get; }

    GoogleCredential? GoogleCredential { get; }

#if NET6_0_OR_GREATER

    public System.Action<Grpc.Net.Client.GrpcChannelOptions>? ConfigureGrpcChannelOptions { get; set;}

#endif

}