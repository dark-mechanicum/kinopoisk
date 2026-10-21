using System;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Enums;
using Jellyfin.Plugin.Kinopoisk.Common;
using Jellyfin.Plugin.Kinopoisk.Configuration;
using Jellyfin.Plugin.Kinopoisk.Extensions;
using Jellyfin.Plugin.Kinopoisk.Providers;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using Xunit;

namespace Jellyfin.Plugin.Kinopoisk.Tests.Providers;

/// <summary>
/// Movie providers end to end, through the Jellyfin interfaces, against the live API.
/// </summary>
public sealed class MovieProvidersLiveTests : IDisposable
{
    private readonly LiveServices _services = new();
    private readonly CancellationToken _token = TestContext.Current.CancellationToken;

    [Fact]
    public async Task GetMetadata_ByKinopoiskId_FillsMovie()
    {
        IRemoteMetadataProvider<Movie, MovieInfo> provider = _services.Create<MovieMetadataProvider>();
        var info = new MovieInfo();
        info.SetKinopoiskId(KnownTitles.MatrixId);

        var result = await provider.GetMetadata(info, _token);

        Assert.True(result.HasMetadata);
        Assert.True(result.QueriedById);
        Assert.Equal(Constants.ProviderName, result.Provider);
        Assert.Equal("ru", result.ResultLanguage);
        var movie = result.Item;
        Assert.Equal("Матрица", movie.Name);
        Assert.Equal("The Matrix", movie.OriginalTitle);
        Assert.Equal(1999, movie.ProductionYear);
        Assert.Equal(new DateTime(1999, 3, 24, 0, 0, 0, DateTimeKind.Utc), movie.PremiereDate);
        Assert.Equal("R", movie.OfficialRating);
        Assert.Contains("Фантастика", movie.Genres);
        Assert.Contains("США", movie.ProductionLocations);
        Assert.NotEmpty(movie.Studios);
        Assert.Equal(KnownTitles.MatrixContentId, movie.GetProviderId(Constants.ContentIdKey));
        Assert.True(movie.TryGetKinopoiskId(out var id) && id == KnownTitles.MatrixId);
        Assert.Contains(result.People, p => p.Type == PersonKind.Actor && p.Name == "Киану Ривз");
        Assert.Contains(result.People, p => p.Type == PersonKind.Director && p.Name == "Лана Вачовски");
        Assert.Contains(result.People, p => p.Role == "Оператор" && p.Name == "Билл Поуп");
        Assert.DoesNotContain(result.People, p => p.Role == "Озвучка");
        Assert.All(result.People, p => Assert.True(p.TryGetKinopoiskId(out _)));
    }

    [Fact]
    public async Task GetMetadata_WithVoiceActorsEnabled_AddsVoiceCast()
    {
        using var services = new LiveServices(new PluginConfiguration { IncludeVoiceActors = true });
        IRemoteMetadataProvider<Movie, MovieInfo> provider = services.Create<MovieMetadataProvider>();
        var info = new MovieInfo();
        info.SetKinopoiskId(KnownTitles.MatrixId);

        var result = await provider.GetMetadata(info, _token);

        Assert.Contains(result.People, p => p is { Type: PersonKind.Actor, Role: "Озвучка" } && !string.IsNullOrWhiteSpace(p.Name));
    }

    [Fact]
    public async Task GetMetadata_WithoutId_FindsMovieByFileName()
    {
        IRemoteMetadataProvider<Movie, MovieInfo> provider = _services.Create<MovieMetadataProvider>();
        var info = new MovieInfo { Path = "/media/movies/The.Matrix.1999.1080p.BluRay.x264-[YTS.AM].mp4" };

        var result = await provider.GetMetadata(info, _token);

        Assert.True(result.HasMetadata);
        Assert.False(result.QueriedById);
        Assert.True(result.Item.TryGetKinopoiskId(out var id));
        Assert.Equal(KnownTitles.MatrixId, id);
    }

    [Fact]
    public async Task GetMetadata_UnknownId_ReturnsEmptyResult()
    {
        IRemoteMetadataProvider<Movie, MovieInfo> provider = _services.Create<MovieMetadataProvider>();
        var info = new MovieInfo();
        info.SetKinopoiskId(KnownTitles.UnknownId);

        var result = await provider.GetMetadata(info, _token);

        Assert.False(result.HasMetadata);
    }

    [Fact]
    public async Task GetSearchResults_ByNameAndYear_PutsThatFilmFirst()
    {
        IRemoteMetadataProvider<Movie, MovieInfo> provider = _services.Create<MovieMetadataProvider>();

        var results = (await provider.GetSearchResults(new MovieInfo { Name = "Матрица", Year = 1999 }, _token)).ToList();

        var matrix = Assert.Single(results, r => r.TryGetKinopoiskId(out var id) && id == KnownTitles.MatrixId);
        Assert.Same(matrix, results[0]);
        Assert.Equal("Матрица (The Matrix)", matrix.Name);
        Assert.Equal(1999, matrix.ProductionYear);
        Assert.StartsWith("https://", matrix.ImageUrl, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GetSearchResults_FindsTitleKinopoiskFilesAsSeries()
    {
        IRemoteMetadataProvider<Movie, MovieInfo> provider = _services.Create<MovieMetadataProvider>();

        var results = (await provider.GetSearchResults(new MovieInfo { Name = "Незнайка на Луне", Year = 1997 }, _token)).ToList();

        Assert.True(results[0].TryGetKinopoiskId(out var id));
        Assert.Equal(KnownTitles.NeznaykaNaLuneId, id);
    }

    [Fact]
    public async Task GetSearchResults_ByKinopoiskSeriesId_ReturnsThatSeries()
    {
        IRemoteMetadataProvider<Movie, MovieInfo> provider = _services.Create<MovieMetadataProvider>();
        var info = new MovieInfo();
        info.SetKinopoiskId(KnownTitles.NeznaykaNaLuneId);

        var result = Assert.Single(await provider.GetSearchResults(info, _token));

        Assert.Equal("Незнайка на Луне", result.Name);
        Assert.Equal(1997, result.ProductionYear);
    }

    [Fact]
    public async Task GetMetadata_ByKinopoiskSeriesId_FillsMovie()
    {
        IRemoteMetadataProvider<Movie, MovieInfo> provider = _services.Create<MovieMetadataProvider>();
        var info = new MovieInfo();
        info.SetKinopoiskId(KnownTitles.NeznaykaNaLuneId);

        var result = await provider.GetMetadata(info, _token);

        Assert.True(result.HasMetadata);
        Assert.Equal("Незнайка на Луне", result.Item.Name);
        Assert.Equal(1997, result.Item.ProductionYear);
        Assert.False(string.IsNullOrWhiteSpace(result.Item.Overview));
    }

    [Fact]
    public async Task GetSearchResults_ById_ReturnsThatFilm()
    {
        IRemoteMetadataProvider<Movie, MovieInfo> provider = _services.Create<MovieMetadataProvider>();
        var info = new MovieInfo();
        info.SetKinopoiskId(KnownTitles.MatrixId);

        var result = Assert.Single(await provider.GetSearchResults(info, _token));

        Assert.Equal("Матрица (The Matrix)", result.Name);
    }

    [Fact]
    public async Task GetImages_ReturnsAllKindsOfDownloadableArtwork()
    {
        var provider = _services.Create<MovieImageProvider>();
        var movie = new Movie();
        movie.SetKinopoiskId(KnownTitles.MatrixId);
        movie.SetContentId(KnownTitles.MatrixContentId);

        var images = (await provider.GetImages(movie, _token)).ToList();

        Assert.Contains(images, i => i.Type == ImageType.Primary);
        Assert.Contains(images, i => i.Type == ImageType.Backdrop);
        Assert.Contains(images, i => i.Type == ImageType.Logo);
        Assert.Equal(images.Count, images.DistinctBy(i => (i.Type, i.Url)).Count());
        Assert.All(images, i => Assert.Equal(Constants.ProviderName, i.ProviderName));

        using var response = await provider.GetImageResponse(images[0].Url, _token);
        response.EnsureSuccessStatusCode();
        Assert.StartsWith("image/", response.Content.Headers.ContentType?.MediaType, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GetImages_ForKinopoiskSeriesId_ReturnsPoster()
    {
        var provider = _services.Create<MovieImageProvider>();
        var movie = new Movie();
        movie.SetKinopoiskId(KnownTitles.NeznaykaNaLuneId);

        var images = await provider.GetImages(movie, _token);

        Assert.Contains(images, i => i.Type == ImageType.Primary);
    }

    [Fact]
    public async Task GetImages_WithoutKinopoiskId_ReturnsNothing()
    {
        var provider = _services.Create<MovieImageProvider>();

        Assert.Empty(await provider.GetImages(new Movie(), _token));
        Assert.True(provider.Supports(new Movie()));
    }

    public void Dispose() => _services.Dispose();
}
