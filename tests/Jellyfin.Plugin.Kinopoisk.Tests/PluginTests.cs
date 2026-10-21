using System;
using System.IO;
using System.Linq;
using Jellyfin.Plugin.Kinopoisk.Common;
using Jellyfin.Plugin.Kinopoisk.Configuration;
using Jellyfin.Plugin.Kinopoisk.Providers;
using MediaBrowser.Controller.Providers;
using Xunit;

namespace Jellyfin.Plugin.Kinopoisk.Tests;

/// <summary>
/// Plugin identity, configuration page and what Jellyfin will discover in the assembly.
/// </summary>
public sealed class PluginTests
{
    [Fact]
    public void Constants_HaveStableIdentity()
    {
        Assert.Equal(Guid.Parse("9b3b77c3-61a9-43a0-bfea-0be2c473e8bd"), Guid.Parse(Constants.PluginGuid));
        Assert.Equal("КиноПоиск", Constants.PluginName);
    }

    [Fact]
    public void ConfigPage_IsEmbedded()
    {
        Assert.Contains("pluginConfigurationPage", ReadConfigPage(), StringComparison.Ordinal);
    }

    [Fact]
    public void ConfigPage_HasFieldForEveryOptionAndUsesPluginGuid()
    {
        var page = ReadConfigPage();

        Assert.Contains(Constants.PluginGuid, page, StringComparison.Ordinal);
        Assert.All(
            typeof(IKinopoiskOptions).GetProperties(),
            option => Assert.Contains($"id=\"{option.Name}\"", page, StringComparison.Ordinal));
    }

    [Fact]
    public void Assembly_ExportsProvidersJellyfinCanDiscover()
    {
        // Jellyfin instantiates every exported, concrete, non-generic class implementing a provider interface.
        var exported = typeof(Plugin).Assembly.GetExportedTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false, IsGenericType: false })
            .ToList();

        Assert.Equal(4, exported.Count(t => typeof(IRemoteMetadataProvider).IsAssignableFrom(t)));
        Assert.Equal(3, exported.Count(t => typeof(IRemoteImageProvider).IsAssignableFrom(t)));
        Assert.Contains(typeof(MovieMetadataProvider), exported);
        Assert.Contains(typeof(SeriesMetadataProvider), exported);
        Assert.Contains(typeof(EpisodeMetadataProvider), exported);
        Assert.Contains(typeof(PersonMetadataProvider), exported);
        Assert.Contains(typeof(MovieImageProvider), exported);
        Assert.Contains(typeof(SeriesImageProvider), exported);
        Assert.Contains(typeof(PersonImageProvider), exported);
        Assert.Contains(typeof(KinopoiskExternalUrlProvider), exported);
        Assert.Contains(typeof(PluginServiceRegistrator), exported);
    }

    private static string ReadConfigPage()
    {
        var resource = $"{typeof(Plugin).Namespace}.Configuration.configPage.html";
        using var stream = typeof(Plugin).Assembly.GetManifestResourceStream(resource);
        Assert.NotNull(stream);
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
