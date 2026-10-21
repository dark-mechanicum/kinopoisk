using System;
using System.Net;
using System.Net.Http;
using Jellyfin.Plugin.Kinopoisk.Api;
using Jellyfin.Plugin.Kinopoisk.Configuration;
using Jellyfin.Plugin.Kinopoisk.Providers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Jellyfin.Plugin.Kinopoisk.Common;

/// <summary>
/// Registers the Kinopoisk services; shared by the Jellyfin registrator and the tests.
/// </summary>
public static class ServiceCollectionExtensions
{
    private const string UserAgent = "Mozilla/5.0 (Jellyfin.Plugin.Kinopoisk)";
    private static readonly TimeSpan _requestTimeout = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan _connectTimeout = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Adds the Kinopoisk client and its HTTP clients. Register an <see cref="IKinopoiskOptions"/> first to
    /// override the plugin configuration (tests).
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The same collection.</returns>
    public static IServiceCollection AddKinopoisk(this IServiceCollection services)
    {
        services.TryAddSingleton<IKinopoiskOptions, PluginOptions>();

        services.AddHttpClient(KinopoiskTransport.GraphQlClientName, client =>
            {
                client.Timeout = _requestTimeout;
                client.DefaultRequestHeaders.Add("service-id", "25");
                client.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgent);
            })
            .ConfigurePrimaryHttpMessageHandler(CreatePrimaryHandler);

        services.AddHttpClient(KinopoiskTransport.RestClientName, client =>
            {
                client.Timeout = _requestTimeout;
                client.DefaultRequestHeaders.Add("X-Requested-With", "XMLHttpRequest");
                client.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgent);
            })
            .ConfigurePrimaryHttpMessageHandler(CreatePrimaryHandler);

        services.AddHttpClient(ImageDownloader.ClientName, client =>
            {
                client.Timeout = _requestTimeout;
                client.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgent);
            })
            .ConfigurePrimaryHttpMessageHandler(CreatePrimaryHandler);

        // Jellyfin's ApplicationHost already registers IMemoryCache.
        services.AddSingleton<KinopoiskTransport>();
        services.AddSingleton<IKinopoiskClient, KinopoiskClient>();
        services.AddSingleton<ImageDownloader>();
        return services;
    }

    /// <summary>
    /// Creates the handler every Kinopoisk client uses: bounded connect time and the IPv4 preference.
    /// </summary>
    /// <param name="services">The service provider.</param>
    /// <returns>The handler.</returns>
    internal static SocketsHttpHandler CreatePrimaryHandler(IServiceProvider services)
    {
        var options = services.GetRequiredService<IKinopoiskOptions>();
        return new SocketsHttpHandler
        {
            AutomaticDecompression = DecompressionMethods.All,
            ConnectTimeout = _connectTimeout,

            // The option is read per connection, so changing it needs no restart.
            ConnectCallback = (context, cancellationToken) =>
                AddressOrderConnector.ConnectAsync(context.DnsEndPoint, options.PreferIpv4, cancellationToken),
        };
    }
}
