using System.Collections.Generic;
using NCoreUtils.Rest;

namespace NCoreUtils.Data;

public interface IRemoteRestTypeConfiguration<TId>
{
    string Endpoint { get; }

    string? HttpClientConfiguration { get; }

    IRestIdHandler<TId>? IdHandler { get; }

    IReadOnlyList<IRestClientErrorHandler>? ErrorHandlers { get; }
}