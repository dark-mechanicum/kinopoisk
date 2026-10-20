using Jellyfin.Plugin.Kinopoisk.Providers;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using Xunit;

namespace Jellyfin.Plugin.Kinopoisk.Tests.Providers;

/// <summary>
/// The Kinopoisk id field in "Edit metadata" and "Identify".
/// </summary>
public sealed class KinopoiskExternalIdTests
{
    private readonly KinopoiskExternalId _externalId = new();

    [Fact]
    public void Key_IsTheStoredProviderIdKey()
        => Assert.Equal("kinopoisk", _externalId.Key);

    [Fact]
    public void Supports_MoviesSeriesAndPersons()
    {
        Assert.True(_externalId.Supports(new Movie()));
        Assert.True(_externalId.Supports(new Series()));
        Assert.True(_externalId.Supports(new Person()));
    }

    [Fact]
    public void Supports_NotEpisodes()
        => Assert.False(_externalId.Supports(new Episode()));
}
