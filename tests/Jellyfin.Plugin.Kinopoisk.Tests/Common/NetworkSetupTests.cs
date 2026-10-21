using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.Kinopoisk.Common;
using Jellyfin.Plugin.Kinopoisk.Configuration;
using Jellyfin.Plugin.Kinopoisk.Providers;
using Jellyfin.Plugin.Kinopoisk.Tests.Fakes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Jellyfin.Plugin.Kinopoisk.Tests.Common;

/// <summary>
/// Network setup of the plugin (KP-07, KP-12): HTTP clients, IPv4 preference and image downloads.
/// </summary>
public sealed class NetworkSetupTests
{
    private static readonly IPAddress _v6First = IPAddress.Parse("2a02:6b8::1");
    private static readonly IPAddress _v4First = IPAddress.Parse("213.180.204.1");
    private static readonly IPAddress _v6Second = IPAddress.Parse("2a02:6b8::2");
    private static readonly IPAddress _v4Second = IPAddress.Parse("213.180.204.2");

    private readonly CancellationToken _token = TestContext.Current.CancellationToken;

    [Fact]
    public void Order_PreferIpv4_PutsIpv4FirstAndKeepsDnsOrderWithinFamily()
        => Assert.Equal(
            [_v4First, _v4Second, _v6First, _v6Second],
            AddressOrderConnector.Order([_v6First, _v4First, _v6Second, _v4Second], preferIpv4: true));

    [Fact]
    public void Order_WithoutPreference_KeepsDnsOrder()
        => Assert.Equal(
            [_v6First, _v4First, _v6Second, _v4Second],
            AddressOrderConnector.Order([_v6First, _v4First, _v6Second, _v4Second], preferIpv4: false));

    [Fact]
    public void ImagesClient_HasBoundedTimeout()
    {
        using var services = new LiveServices();

        var client = services.HttpClientFactory.CreateClient(ImageDownloader.ClientName);

        Assert.Equal(TimeSpan.FromSeconds(30), client.Timeout);
    }

    [Fact]
    public void PrimaryHandler_BoundsConnectTimeAndUsesAddressOrder()
    {
        using var services = new LiveServices();

        using var handler = ServiceCollectionExtensions.CreatePrimaryHandler(services.Services);

        Assert.Equal(TimeSpan.FromSeconds(10), handler.ConnectTimeout);
        Assert.NotNull(handler.ConnectCallback);
        Assert.Equal(DecompressionMethods.All, handler.AutomaticDecompression);
    }

    [Fact]
    public void Options_DefaultToPluginConfigurationDefaults()
    {
        using var services = new LiveServices();

        var options = services.Services.GetRequiredService<IKinopoiskOptions>();

        Assert.IsType<PluginOptions>(options);
        Assert.Equal(30, options.CrewLimit);
        Assert.False(options.PreferIpv4);
    }

    [Fact]
    public async Task ImageDownloader_RetriesConnectionFailureOnce()
    {
        using var handler = new QueuedHttpMessageHandler()
            .Throw(new HttpRequestException("Resource temporarily unavailable"))
            .Respond(HttpStatusCode.OK);
        var downloader = new ImageDownloader(new StubHttpClientFactory(handler), NullLogger<ImageDownloader>.Instance);

        using var response = await downloader.GetAsync("https://avatars.mds.yandex.net/x", _token);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(2, handler.Calls);
    }

    [Fact]
    public async Task ImageDownloader_ReturnsNotFoundAsIs()
    {
        using var handler = new QueuedHttpMessageHandler().Respond(HttpStatusCode.NotFound);
        var downloader = new ImageDownloader(new StubHttpClientFactory(handler), NullLogger<ImageDownloader>.Instance);

        using var response = await downloader.GetAsync("https://avatars.mds.yandex.net/x", _token);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(1, handler.Calls);
    }
}
