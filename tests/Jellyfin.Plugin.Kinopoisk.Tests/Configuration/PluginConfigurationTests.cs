using Jellyfin.Plugin.Kinopoisk.Api.Models;
using Jellyfin.Plugin.Kinopoisk.Configuration;
using Xunit;

namespace Jellyfin.Plugin.Kinopoisk.Tests.Configuration;

/// <summary>
/// Settings defaults and validation (KP-12).
/// </summary>
public sealed class PluginConfigurationTests
{
    [Fact]
    public void Defaults_KeepTheBehaviourOfReleasesWithoutSettings()
    {
        var configuration = new PluginConfiguration();

        Assert.Equal(30, configuration.CrewLimit);
        Assert.True(configuration.CapitalizeGenres);
        Assert.Equal(0, configuration.MinRequestIntervalMs);
        Assert.False(configuration.PreferIpv4);
        Assert.False(configuration.IncludeVoiceActors);
        Assert.Equal(RatingSource.Kinopoisk, configuration.CommunityRating);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(-5, 1)]
    [InlineData(50, 50)]
    [InlineData(500, 200)]
    public void CrewLimit_IsClampedToSupportedRange(int value, int expected)
        => Assert.Equal(expected, new PluginConfiguration { CrewLimit = value }.CrewLimit);

    [Theory]
    [InlineData(-5, 0)]
    [InlineData(100, 100)]
    [InlineData(60_000, 5000)]
    public void MinRequestIntervalMs_IsClampedToSupportedRange(int value, int expected)
        => Assert.Equal(expected, new PluginConfiguration { MinRequestIntervalMs = value }.MinRequestIntervalMs);
}
