using System;
using System.Net;
using System.Net.Http;
using Jellyfin.Plugin.Kinopoisk.Api;
using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Plugin.Kinopoisk.Common;

/// <summary>
/// Registers the Kinopoisk services; shared by the Jellyfin registrator and the tests.
/// </summary>
public static class ServiceCollectionExtensions
{
    private const string UserAgent = "Mozilla/5.0 (Jellyfin.Plugin.Kinopoisk)";
    private static readonly TimeSpan _requestTimeout = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Adds the Kinopoisk client and its HTTP clients.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The same collection.</returns>
    public static IServiceCollection AddKinopoisk(this IServiceCollection services)
    {
        services.AddHttpClient(KinopoiskTransport.GraphQlClientName, client =>
        {
            client.Timeout = _requestTimeout;
            client.DefaultRequestHeaders.Add("service-id", "25");
            client.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgent);
        });

        services.AddHttpClient(KinopoiskTransport.RestClientName, client =>
            {
                client.Timeout = _requestTimeout;
                client.DefaultRequestHeaders.Add("X-Requested-With", "XMLHttpRequest");
                client.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgent);
            })
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
            {
                AutomaticDecompression = DecompressionMethods.All,
            });

        // Jellyfin's ApplicationHost already registers IMemoryCache and the default HttpClient.
        services.AddSingleton<KinopoiskTransport>();
        services.AddSingleton<IKinopoiskClient, KinopoiskClient>();
        return services;
    }
}
