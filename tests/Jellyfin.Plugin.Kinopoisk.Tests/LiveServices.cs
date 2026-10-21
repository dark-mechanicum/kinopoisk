using System;
using System.Net.Http;
using Jellyfin.Plugin.Kinopoisk.Api;
using Jellyfin.Plugin.Kinopoisk.Common;
using Jellyfin.Plugin.Kinopoisk.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Plugin.Kinopoisk.Tests;

/// <summary>
/// The real plugin services, wired like Jellyfin does it, talking to the live Kinopoisk API.
/// A fresh instance (and therefore a fresh cache) is created per test class.
/// </summary>
public sealed class LiveServices : IDisposable
{
    private readonly ServiceProvider _provider;

    /// <summary>
    /// Initializes a new instance of the <see cref="LiveServices"/> class.
    /// </summary>
    /// <param name="options">Settings to use instead of the defaults.</param>
    public LiveServices(IKinopoiskOptions? options = null)
    {
        var services = new ServiceCollection()
            .AddLogging()
            .AddMemoryCache();

        if (options is not null)
        {
            services.AddSingleton(options);
        }

        services.AddKinopoisk();

        _provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
    }

    public IKinopoiskClient Client => _provider.GetRequiredService<IKinopoiskClient>();

    public KinopoiskTransport Transport => _provider.GetRequiredService<KinopoiskTransport>();

    public IHttpClientFactory HttpClientFactory => _provider.GetRequiredService<IHttpClientFactory>();

    public IServiceProvider Services => _provider;

    /// <summary>
    /// Creates a plugin type (provider) with its real dependencies.
    /// </summary>
    /// <typeparam name="T">The type.</typeparam>
    /// <returns>The instance.</returns>
    public T Create<T>() => ActivatorUtilities.CreateInstance<T>(_provider);

    public void Dispose() => _provider.Dispose();
}
