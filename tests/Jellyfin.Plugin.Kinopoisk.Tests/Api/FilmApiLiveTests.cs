using System;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.Kinopoisk.Api;
using Jellyfin.Plugin.Kinopoisk.Api.Models;
using Xunit;

namespace Jellyfin.Plugin.Kinopoisk.Tests.Api;

/// <summary>
/// Contract tests of the film queries against the live Kinopoisk API.
/// A failure here means Kinopoisk changed and the plugin must follow.
/// </summary>
public sealed class FilmApiLiveTests : IDisposable
{
    private readonly LiveServices _services = new();
    private readonly CancellationToken _token = TestContext.Current.CancellationToken;

    private IKinopoiskClient Client => _services.Client;

    [Fact]
    public async Task SuggestSearch_FindsMatrixByTitleAndYear()
    {
        var results = await Client.SearchFilmsAsync("Матрица 1999", _token);

        var matrix = Assert.Single(results, f => f.Id == KnownTitles.MatrixId);
        Assert.Equal("Матрица", matrix.Title?.Russian);
        Assert.Equal("The Matrix", matrix.Title?.Original);
        Assert.Equal(1999, matrix.Year);
        Assert.True(matrix.IsFilm);
        Assert.Equal(KnownTitles.MatrixContentId, matrix.ContentId);
    }

    [Fact]
    public async Task FilmBaseInfo_ReturnsFullMatrixMetadata()
    {
        var film = await Client.GetFilmAsync(KnownTitles.MatrixId, _token);

        Assert.Equal(KnownTitles.MatrixId, film.Id);
        Assert.Equal("Матрица", film.Title?.Russian);
        Assert.Equal(1999, film.ProductionYear);
        Assert.Equal("1999-03-24", film.WorldPremiere?.IncompleteDate?.Date);
        Assert.False(string.IsNullOrWhiteSpace(film.Synopsis));
        Assert.Contains(film.Genres, g => g.Name == "фантастика");
        Assert.Contains(film.Countries, c => c.Name == "США");
        Assert.Equal("R", film.Restriction?.OfficialRating);
        Assert.InRange(film.Rating?.Community ?? 0, 7f, 10f);
        Assert.InRange(film.Rating?.Critics ?? 0, 1f, 100f);
        Assert.Contains(film.Actors?.Items ?? [], m => m.Person?.Id == KnownTitles.KeanuReevesId);
        Assert.Contains(film.Directors?.Items ?? [], m => m.Person?.Id == KnownTitles.LanaWachowskiId);
        Assert.NotEmpty(film.Writers?.Items ?? []);
        Assert.Contains(film.GetArtwork(), i => i.Url is not null);
    }

    [Fact]
    public async Task FilmBaseInfo_UnknownIdThrowsWithKinopoiskMessage()
    {
        var ex = await Assert.ThrowsAsync<KinopoiskApiException>(() => Client.GetFilmAsync(KnownTitles.UnknownId, _token));

        Assert.Contains("There is no film", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task FilmPage_ReturnsGalleryForFilm()
    {
        var page = await Client.GetFilmPageAsync(KnownTitles.MatrixContentId, _token);

        Assert.Equal(KnownTitles.MatrixId, page.Id);
        Assert.NotNull(page.Gallery?.Posters?.Vertical?.Url);
        Assert.NotNull(page.Gallery?.Logos?.Horizontal?.Url);
    }

    [Theory]
    [InlineData(FilmImageType.Poster)]
    [InlineData(FilmImageType.Cover)]
    [InlineData(FilmImageType.Still)]
    [InlineData(FilmImageType.Wallpaper)]
    [InlineData(FilmImageType.Screenshot)]
    [InlineData(FilmImageType.FanArt)]
    public async Task MovieImagesItems_ReturnsEveryImageCategory(FilmImageType type)
    {
        var images = await Client.GetImagesAsync(KnownTitles.MatrixId, type, _token);

        Assert.NotEmpty(images.Items ?? []);
        Assert.All(images.Items ?? [], i => Assert.Equal(type, i.Type));
        Assert.All(images.Items ?? [], i => Assert.StartsWith("https://", i.Image?.Url, StringComparison.Ordinal));
    }

    [Fact]
    public async Task Cache_ReturnsSameInstanceForRepeatedCall()
    {
        var first = await Client.GetFilmAsync(KnownTitles.MatrixId, _token);
        var second = await Client.GetFilmAsync(KnownTitles.MatrixId, _token);

        Assert.Same(first, second);
    }

    public void Dispose() => _services.Dispose();
}
