using System.Linq;
using Jellyfin.Plugin.Kinopoisk.Extensions;
using Jellyfin.Plugin.Kinopoisk.Providers;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using Xunit;

namespace Jellyfin.Plugin.Kinopoisk.Tests.Providers;

/// <summary>
/// kinopoisk.ru links.
/// </summary>
public sealed class KinopoiskExternalUrlProviderTests
{
    private readonly KinopoiskExternalUrlProvider _provider = new();

    [Fact]
    public void GetExternalUrls_MovieLinksToFilmPage()
        => Assert.Equal(["https://www.kinopoisk.ru/film/301/"], _provider.GetExternalUrls(WithId(new Movie(), 301)));

    [Fact]
    public void GetExternalUrls_SeriesLinksToSeriesPage()
        => Assert.Equal(["https://www.kinopoisk.ru/series/464963/"], _provider.GetExternalUrls(WithId(new Series(), 464963)));

    [Fact]
    public void GetExternalUrls_PersonLinksToNamePage()
        => Assert.Equal(["https://www.kinopoisk.ru/name/7836/"], _provider.GetExternalUrls(WithId(new Person(), 7836)));

    [Fact]
    public void GetExternalUrls_NothingWithoutId()
        => Assert.Empty(_provider.GetExternalUrls(new Movie()));

    [Fact]
    public void GetExternalUrls_NothingForUnsupportedType()
        => Assert.Empty(_provider.GetExternalUrls(WithId(new Episode(), 1)));

    private static BaseItem WithId(BaseItem item, int id)
    {
        item.SetKinopoiskId(id);
        return item;
    }
}
