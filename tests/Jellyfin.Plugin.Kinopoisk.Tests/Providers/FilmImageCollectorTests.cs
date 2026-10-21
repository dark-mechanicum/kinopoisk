using System.Linq;
using System.Text.Json;
using Jellyfin.Plugin.Kinopoisk.Api;
using Jellyfin.Plugin.Kinopoisk.Api.Models;
using Jellyfin.Plugin.Kinopoisk.Providers;
using MediaBrowser.Model.Entities;
using Xunit;

namespace Jellyfin.Plugin.Kinopoisk.Tests.Providers;

/// <summary>
/// Typed images with categories the plugin does not know (KP-01).
/// </summary>
public sealed class FilmImageCollectorTests
{
    [Fact]
    public void ToArtwork_UnknownImageTypeIsSkippedAndOthersKept()
    {
        var root = JsonDocument.Parse("""
            {"items":[
                {"type":"POSTER","image":{"avatarsUrl":"//h/p"}},
                {"type":"SOMETHING_NEW","image":{"avatarsUrl":"//h/q"}}
            ]}
            """).RootElement;

        var images = KinopoiskJson.Deserialize<FilmImages>(root, "test");
        var artwork = FilmImageCollector.ToArtwork(images).ToList();

        var image = Assert.Single(artwork);
        Assert.Equal((ImageType.Primary, "https://h/p/576x"), image);
    }

    [Fact]
    public void ToArtwork_MapsBackdropCategories()
    {
        var images = new FilmImages([new FilmImage("STILL", new Image { AvatarsUrl = "//h/s" })]);

        Assert.Equal(ImageType.Backdrop, Assert.Single(FilmImageCollector.ToArtwork(images)).Type);
    }

    [Fact]
    public void RequestedTypes_AreTheKnownCategoriesOnce()
        => Assert.Equal(
            System.Enum.GetValues<FilmImageType>().Order(),
            FilmImageCollector.RequestedTypes.Order());
}
