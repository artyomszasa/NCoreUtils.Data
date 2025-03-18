using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization.Metadata;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NCoreUtils.Data.Rest;
using NCoreUtils.Rest;
using NCoreUtils.Rest.Internal;

namespace NCoreUtils.Data;

public static class ServiceCollectionDataRestExtensions
{
    private static RemoteRestTypeConfiguration<TId>? GetRestClientConfiguration<TId>(this IConfiguration configuration)
    {
        if (configuration is IConfigurationSection section)
        {
            var endpoint = section[nameof(IRemoteRestTypeConfiguration<TId>.Endpoint)]
                ?? throw new InvalidOperationException("Endpoint must be specified in remote rest type configuration");
            var httpClient = section["HttpClient"];
            return new RemoteRestTypeConfiguration<TId>(endpoint, httpClient);
        }
        return default;
    }

    public static IServiceCollection AddRestDataRepositoryContext(this IServiceCollection services, IRestClientJsonTypeInfoResolver? jsonTypeInfoResolver = default)
    {
        services.AddCommonRestClientServices(jsonTypeInfoResolver);
        services.TryAddScoped<RestDataRepositoryContext>();
        services.TryAddScoped<IDataRepositoryContext>(serviceProvider => serviceProvider.GetRequiredService<RestDataRepositoryContext>());
        return services;
    }

    public static IServiceCollection AddRestDataRepositoryContext(this IServiceCollection services, IJsonTypeInfoResolver jsonTypeInfoResolver)
        => services.AddRestDataRepositoryContext(new RestClientJsonTypeInfoResolver(jsonTypeInfoResolver));

    private static class GetRequiredServiceHelper<
        TRepository,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] TData,
        TId>
        where TRepository : RestDataRepository<TData, TId>
        where TData : IHasId<TId>
    {
        public static Func<IServiceProvider, IDataRepository<TData, TId>> GetAsIDataRepositoryOfTDataAndTId = DoGetAsIDataRepositoryOfTDataAndTId;

        public static Func<IServiceProvider, IDataRepository<TData>> GetAsIDataRepositoryOfTData = DoGetAsIDataRepositoryOfTData;

        private static IDataRepository<TData, TId> DoGetAsIDataRepositoryOfTDataAndTId(IServiceProvider serviceProvider)
            => serviceProvider.GetRequiredService<TRepository>();

        private static IDataRepository<TData> DoGetAsIDataRepositoryOfTData(IServiceProvider serviceProvider)
            => serviceProvider.GetRequiredService<TRepository>();
    }

    public static IServiceCollection AddRestDataRepository<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TRepository,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TData,
        TId>(
        this IServiceCollection services,
        IRemoteRestTypeConfiguration<TId> configuration)
        where TRepository : RestDataRepository<TData, TId>
        where TData : class, IHasId<TId>
        where TId : IEquatable<TId>
        => services
            .AddRemoteRestType<TData, TId>(configuration.Endpoint, configuration.HttpClientConfiguration, configuration.IdHandler, configuration.ErrorHandlers)
            .AddScoped<TRepository>()
            .AddScoped(GetRequiredServiceHelper<TRepository, TData, TId>.GetAsIDataRepositoryOfTDataAndTId)
            .AddScoped(GetRequiredServiceHelper<TRepository, TData, TId>.GetAsIDataRepositoryOfTData);

    public static IServiceCollection AddRestDataRepository<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TData, TId>(
        this IServiceCollection services,
        IRemoteRestTypeConfiguration<TId> configuration)
        where TData : class, IHasId<TId>
        where TId : IEquatable<TId>
        => services.AddRestDataRepository<RestDataRepository<TData, TId>, TData, TId>(configuration);

    public static IServiceCollection AddRestDataRepository<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TRepository,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TData,
        TId>(
        this IServiceCollection services,
        string endpoint,
        string? httpClient = default,
        IRestIdHandler<TId>? idHandler = default,
        IReadOnlyList<IRestClientErrorHandler>? errorHandlers = default)
        where TRepository : RestDataRepository<TData, TId>
        where TData : class, IHasId<TId>
        where TId : IEquatable<TId>
        => services.AddRestDataRepository<TRepository, TData, TId>(
            new RemoteRestTypeConfiguration<TId>(endpoint, httpClient, idHandler, errorHandlers)
        );

    public static IServiceCollection AddRestDataRepository<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TData, TId>(
        this IServiceCollection services,
        string endpoint,
        string? httpClient = default,
        IRestIdHandler<TId>? idHandler = default,
        IReadOnlyList<IRestClientErrorHandler>? errorHandlers = default)
        where TData : class, IHasId<TId>
        where TId : IEquatable<TId>
        => services.AddRestDataRepository<RestDataRepository<TData, TId>, TData, TId>(
            new RemoteRestTypeConfiguration<TId>(endpoint, httpClient, idHandler, errorHandlers)
        );

    public static IServiceCollection AddRestDataRepository<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TRepository,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TData,
        TId>(
        this IServiceCollection services,
        IConfiguration configuration)
        where TRepository : RestDataRepository<TData, TId>
        where TData : class, IHasId<TId>
        where TId : IEquatable<TId>
        => services.AddRestDataRepository<TRepository, TData, TId>(
            configuration.GetRestClientConfiguration<TId>()
            ?? throw new InvalidOperationException($"No REST client configuration found for {typeof(TData)}.")
        );
}