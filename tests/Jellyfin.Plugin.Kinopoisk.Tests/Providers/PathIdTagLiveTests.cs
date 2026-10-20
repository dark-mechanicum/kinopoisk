using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.Kinopoisk.Common;
using Jellyfin.Plugin.Kinopoisk.Extensions;
using Jellyfin.Plugin.Kinopoisk.Providers;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using Xunit;

namespace Jellyfin.Plugin.Kinopoisk.Tests.Providers;

/// <summary>
/// Explicit <c>kp1234</c> tags in file and folder names, resolved end to end against the live API.
/// </summary>
public sealed class PathIdTagLiveTests : IDisposable
{
    private readonly LiveServices _services = new();
    private readonly CancellationToken _token = TestContext.Current.CancellationToken;

    [Theory]
    [InlineData("/media/movies/Something Else (2020) [kp301]/movie.mkv")]
    [InlineData("/media/movies/Something.Else.2020.kp-301.1080p.mkv")]
    [InlineData("/media/movies/Something Else {kpid=301}/video.mkv")]
    public async Task Movie_TagInPathWinsOverMisleadingName(string path)
    {
        IRemoteMetadataProvider<Movie, MovieInfo> provider = _services.Create<MovieMetadataProvider>();

        var result = await provider.GetMetadata(new MovieInfo { Name = "Something Else", Year = 2020, Path = path }, _token);

        Assert.True(result.HasMetadata);
        Assert.True(result.QueriedById);
        Assert.Equal("Матрица", result.Item.Name);
        Assert.True(result.Item.TryGetKinopoiskId(out var id));
        Assert.Equal(KnownTitles.MatrixId, id);
    }

    [Fact]
    public async Task Movie_TagInPathReplacesStoredId()
    {
        IRemoteMetadataProvider<Movie, MovieInfo> provider = _services.Create<MovieMetadataProvider>();
        var info = new MovieInfo { Path = "/media/movies/The Matrix [kp-301]/movie.mkv" };
        info.SetKinopoiskId(KnownTitles.GameOfThronesId);

        var result = await provider.GetMetadata(info, _token);

        Assert.True(result.Item.TryGetKinopoiskId(out var id));
        Assert.Equal(KnownTitles.MatrixId, id);
    }

    [Fact]
    public async Task Movie_UnknownTaggedIdIsNotReplacedByNameSearch()
    {
        IRemoteMetadataProvider<Movie, MovieInfo> provider = _services.Create<MovieMetadataProvider>();
        var path = string.Create(CultureInfo.InvariantCulture, $"/media/movies/Матрица (1999) [kp{KnownTitles.UnknownId}]/movie.mkv");

        var result = await provider.GetMetadata(new MovieInfo { Name = "Матрица", Year = 1999, Path = path }, _token);

        Assert.False(result.HasMetadata);
    }

    [Fact]
    public async Task Series_TagInFolderNameResolvesSeries()
    {
        IRemoteMetadataProvider<Series, SeriesInfo> provider = _services.Create<SeriesMetadataProvider>();

        var result = await provider.GetMetadata(new SeriesInfo { Path = "/media/tv/Игра престолов [kp-464963]/" }, _token);

        Assert.True(result.HasMetadata);
        Assert.True(result.QueriedById);
        Assert.Equal("Игра престолов", result.Item.Name);
        Assert.False(string.IsNullOrWhiteSpace(result.Item.GetProviderId(Constants.ContentIdKey)));
    }

    public void Dispose() => _services.Dispose();
}
