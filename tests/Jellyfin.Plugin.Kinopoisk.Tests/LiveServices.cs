using System;
using System.Net.Http;
using Jellyfin.Plugin.Kinopoisk.Api;
using Jellyfin.Plugin.Kinopoisk.Common;
using MediaBrowser.Common.Net;
using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Plugin.Kinopoisk.Tests;

/// <summary>
/// The real plugin services, wired like Jellyfin does it, talking to the live Kinopoisk API.
/// A fresh instance (and therefore a fresh cache) is created per test class.
/// </summary>
public sealed class LiveServices : IDisposable
{
    private readonly ServiceProvider _provider;

    public LiveServices()
    {
        var services = new ServiceCollection()
            .AddLogging()
            .AddMemoryCache();

        // Registered by Jellyfin's ApplicationHost in production.
        services.AddHttpClient(NamedClient.Default);
        services.AddKinopoisk();

        _provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
    }

    public IKinopoiskClient Client => _provider.GetRequiredService<IKinopoiskClient>();

    public KinopoiskTransport Transport => _provider.GetRequiredService<KinopoiskTransport>();

    public IHttpClientFactory HttpClientFactory => _provider.GetRequiredService<IHttpClientFactory>();

    /// <summary>
    /// Creates a plugin type (provider) with its real dependencies.
    /// </summary>
    /// <typeparam name="T">The type.</typeparam>
    /// <returns>The instance.</returns>
    public T Create<T>() => ActivatorUtilities.CreateInstance<T>(_provider);

    public void Dispose() => _provider.Dispose();
}
