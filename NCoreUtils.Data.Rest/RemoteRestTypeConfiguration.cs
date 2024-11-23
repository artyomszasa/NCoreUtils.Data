using System.Collections.Generic;
using NCoreUtils.Rest;

namespace NCoreUtils.Data;

public sealed record RemoteRestTypeConfiguration<TId>(
    string Endpoint,
    string? HttpClientConfiguration = default,
    IRestIdHandler<TId>? IdHandler = default,
    IReadOnlyList<IRestClientErrorHandler>? ErrorHandlers = default
) : IRemoteRestTypeConfiguration<TId>;