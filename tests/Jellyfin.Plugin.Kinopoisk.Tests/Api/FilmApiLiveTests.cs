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
        var results = await Client.SearchFilmsAsync("Матрица", 1999, _token);

        var matrix = Assert.Single(results, f => f.Id == KnownTitles.MatrixId);
        Assert.Same(matrix, results[0]);
        Assert.Equal("Матрица", matrix.Title?.Russian);
        Assert.Equal("The Matrix", matrix.Title?.Original);
        Assert.Equal(1999, matrix.Year);
        Assert.True(matrix.IsFilm);
        Assert.Equal(KnownTitles.MatrixContentId, matrix.ContentId);
    }

    [Fact]
    public async Task SuggestSearch_YearMovesTitleOfThatYearFirst()
    {
        var results = await Client.SearchFilmsAsync("Незнайка", 1961, _token);

        Assert.Equal(KnownTitles.NeznaykaUchitsyaId, results[0].Id);
    }

    [Fact]
    public async Task SuggestSearch_WrongYearStillFindsTitle()
    {
        // Kinopoisk itself finds nothing for "Незнайка учится 1962": the film is from 1961.
        var results = await Client.SearchFilmsAsync("Незнайка учится", 1962, _token);

        Assert.Contains(results, f => f.Id == KnownTitles.NeznaykaUchitsyaId);
    }

    [Fact]
    public async Task GetTitle_ReturnsFilmWhenKinopoiskHasOne()
    {
        var title = await Client.GetTitleAsync(KnownTitles.MatrixId, _token);

        Assert.True(title.IsFilm);
        Assert.Equal("Матрица", title.Title?.Russian);
    }

    [Fact]
    public async Task GetTitle_FallsBackToSeriesWhenKinopoiskHasNoSuchFilm()
    {
        var title = await Client.GetTitleAsync(KnownTitles.NeznaykaNaLuneId, _token);

        Assert.False(title.IsFilm);
        Assert.Equal(KnownTitles.NeznaykaNaLuneId, title.Id);
        Assert.Equal("Незнайка на Луне", title.Title?.Russian);
        Assert.Equal(1997, title.Year);
    }

    [Fact]
    public async Task GetTitle_UnknownIdThrowsNotFound()
    {
        var ex = await Assert.ThrowsAsync<KinopoiskApiException>(() => Client.GetTitleAsync(KnownTitles.UnknownId, _token));

        Assert.Equal(KinopoiskErrorKind.NotFound, ex.Kind);
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
        Assert.InRange(film.Rating?.GetCommunity(RatingSource.Kinopoisk) ?? 0, 7f, 10f);
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
        Assert.Equal(KinopoiskErrorKind.NotFound, ex.Kind);
    }

    [Fact]
    public async Task FilmBaseInfo_ReturnsCinematographerAndDistributors()
    {
        var film = await Client.GetFilmAsync(KnownTitles.MatrixId, _token);

        Assert.Contains(film.Operators?.Items ?? [], m => m.Person?.Id == KnownTitles.BillPopeId);
        Assert.NotEmpty(film.Designers?.Items ?? []);
        Assert.NotEmpty(film.Distribution?.CompanyNames ?? []);
        Assert.Empty(film.VoiceOverActors?.Items ?? []);
    }

    [Fact]
    public async Task FilmPage_ReturnsGalleryForFilm()
    {
        var page = await Client.GetFilmPageAsync(KnownTitles.MatrixContentId, _token);

        Assert.Equal(KnownTitles.MatrixId, page.Id);
        Assert.NotNull(page.Gallery?.Posters?.Vertical?.Url);
        Assert.NotNull(page.Gallery?.Logos?.Horizontal?.Url);
    }

    [Fact]
    public async Task FilmBaseInfo_ReturnsTop250Position()
    {
        var film = await Client.GetFilmAsync(KnownTitles.MatrixId, _token);

        Assert.True(film.RatingLists?.IsInTop250);
        Assert.Equal("top250", film.RatingLists?.Top250?.MovieListSlug);
        Assert.InRange(film.RatingLists?.Top250?.Position ?? 0, 1, 250);
    }

    [Fact]
    public async Task FilmBaseInfo_FilmOutsideTop250HasNoPosition()
    {
        var film = await Client.GetFilmAsync(KnownTitles.NeznaykaUchitsyaId, _token);

        Assert.NotNull(film.RatingLists);
        Assert.False(film.RatingLists.IsInTop250);
    }

    [Fact]
    public async Task FilmBaseInfo_FilmHasNoSeriesEpisodeLength()
    {
        var film = await Client.GetFilmAsync(KnownTitles.MatrixId, _token);

        Assert.Null(film.SeriesDuration);
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
        Assert.All(images.Items ?? [], i => Assert.Equal(type, i.Category));
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
