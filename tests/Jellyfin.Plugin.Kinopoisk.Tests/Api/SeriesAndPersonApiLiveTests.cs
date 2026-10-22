using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.Kinopoisk.Api;
using Jellyfin.Plugin.Kinopoisk.Common;
using Xunit;

namespace Jellyfin.Plugin.Kinopoisk.Tests.Api;

/// <summary>
/// Contract tests of the series, episode and person queries against the live Kinopoisk API.
/// A failure here means Kinopoisk changed and the plugin must follow.
/// </summary>
public sealed class SeriesAndPersonApiLiveTests : IDisposable
{
    private readonly LiveServices _services = new();
    private readonly CancellationToken _token = TestContext.Current.CancellationToken;

    private IKinopoiskClient Client => _services.Client;

    [Fact]
    public async Task SuggestSearch_MarksSeriesAsNotFilm()
    {
        var results = await Client.SearchFilmsAsync("Игра престолов", null, _token);

        var series = Assert.Single(results, f => f.Id == KnownTitles.GameOfThronesId);
        Assert.False(series.IsFilm);
    }

    [Fact]
    public async Task TvSeriesBaseInfo_ReturnsGameOfThrones()
    {
        var series = await Client.GetSeriesAsync(KnownTitles.GameOfThronesId, _token);

        Assert.Equal("Игра престолов", series.Title?.Russian);
        Assert.Equal("Game of Thrones", series.Title?.Original);
        Assert.Equal(2011, series.Year);
        Assert.Contains(series.ReleaseYears ?? [], y => y is { Start: 2011, End: 2019 });
        Assert.Contains(series.Genres, g => g.Name == "фэнтези");
        Assert.Equal("18+", series.Restriction?.OfficialRating);
        Assert.NotEmpty(series.Actors?.Items ?? []);
        Assert.False(string.IsNullOrWhiteSpace(series.ContentId));
    }

    [Fact]
    public async Task TvSeriesEpisodes_ReturnsFinale()
    {
        var episodes = await Client.GetLatestEpisodesAsync(KnownTitles.GameOfThronesId, _token);

        var finale = Assert.Single(episodes, e => e is { Number: 6, Season.Number: 8 });
        Assert.Equal("Железный Трон", finale.Title?.Russian);
        Assert.Equal("The Iron Throne", finale.Title?.Original);
        Assert.Equal("2019-05-19", finale.ReleaseDate?.Date);
    }

    [Fact]
    [Trait("Category", StreamingCatalog.Category)]
    public async Task FilmPage_ReturnsSeasonsWithContentIds()
    {
        var page = await Client.GetFilmPageAsync(KnownTitles.MetodContentId, _token);

        Assert.Equal(KnownTitles.MetodId, page.Id);
        StreamingCatalog.RequireAvailable(page.Seasons?.Items is { Count: > 0 });
        var first = Assert.Single(page.Seasons!.Items!, s => s.Number == 1);
        Assert.Equal(KnownTitles.MetodSeason1ContentId, first.ContentId);
    }

    [Fact]
    [Trait("Category", StreamingCatalog.Category)]
    public async Task SerialStructureSeason_ReturnsWholeSeason()
    {
        var episodes = await Client.GetSeasonEpisodesAsync(KnownTitles.MetodSeason1ContentId, _token);

        StreamingCatalog.RequireAvailable(episodes.Count > 0);
        Assert.Equal(Enumerable.Range(1, 16), episodes.Select(e => e.Number));
        Assert.All(episodes, e => Assert.Equal(1, e.Season?.Number));
        Assert.All(episodes, e => Assert.False(string.IsNullOrWhiteSpace(e.Synopsis)));
    }

    [Fact]
    public async Task SuggestSearch_FindsPerson()
    {
        var results = await Client.SearchPersonsAsync("Киану Ривз", _token);

        var keanu = Assert.Single(results, p => p.Id == KnownTitles.KeanuReevesId);
        Assert.Equal("Киану Ривз", keanu.Name);
        Assert.Equal("Keanu Reeves", keanu.OriginalName);
    }

    [Fact]
    public async Task PersonApi_ReturnsKeanuReeves()
    {
        var person = await Client.GetPersonAsync(KnownTitles.KeanuReevesId, _token);

        Assert.Equal(KnownTitles.KeanuReevesId, person.Id);
        Assert.Equal("Киану Ривз", person.Name);
        Assert.Equal("Keanu Reeves", person.OriginalName);
        Assert.Equal("1964-09-02", person.BirthDate);
        Assert.False(string.IsNullOrWhiteSpace(person.BirthPlace));
        Assert.Contains(person.GetArtwork(), i => i.Url is not null);
    }

    [Fact]
    public async Task PersonApi_UnknownIdThrowsNotFound()
    {
        var ex = await Assert.ThrowsAsync<KinopoiskApiException>(() => Client.GetPersonAsync(KnownTitles.UnknownId, _token));

        Assert.Equal(KinopoiskErrorKind.NotFound, ex.Kind);
    }

    [Fact]
    public async Task PersonApi_PhotolessPersonHasNoArtwork()
    {
        var person = await Client.GetPersonAsync(KnownTitles.PhotolessPersonId, _token);

        Assert.False(string.IsNullOrWhiteSpace(person.Name));
        Assert.DoesNotContain(person.GetArtwork(), i => i.Url is not null);
    }

    [Fact]
    public async Task TvSeriesBaseInfo_ReturnsNetworkAsDistributor()
    {
        var series = await Client.GetSeriesAsync(KnownTitles.GameOfThronesId, _token);

        Assert.Contains("HBO", series.Distribution?.CompanyNames ?? []);
    }

    [Theory]
    [InlineData(KnownTitles.GameOfThronesId, 50, 60)]
    [InlineData(KnownTitles.ChernobylId, 55, 65)]
    [InlineData(KnownTitles.SimpsonsId, 20, 25)]
    public async Task TvSeriesBaseInfo_ReturnsEpisodeLength(int seriesId, int minMinutes, int maxMinutes)
    {
        var series = await Client.GetSeriesAsync(seriesId, _token);

        Assert.InRange(series.SeriesDuration ?? 0, minMinutes, maxMinutes);
    }

    [Fact]
    public async Task TvSeriesBaseInfo_RunningSeriesHasOpenReleaseYears()
    {
        var series = await Client.GetSeriesAsync(KnownTitles.SimpsonsId, _token);

        var run = Assert.Single(series.ReleaseYears ?? []);
        Assert.Equal(1989, run.Start);
        Assert.Null(run.End);
    }

    [Fact]
    public async Task TvSeriesBaseInfo_ReturnsSeriesTop250Position()
    {
        var series = await Client.GetSeriesAsync(KnownTitles.GameOfThronesId, _token);

        Assert.True(series.RatingLists?.IsInTop250);
        Assert.Equal("series-top250", series.RatingLists?.Top250?.MovieListSlug);
        Assert.InRange(series.RatingLists?.Top250?.Position ?? 0, 1, 250);
    }

    [Fact]
    public async Task TvSeriesBaseInfo_SeriesOutsideTop250HasNoPosition()
    {
        var series = await Client.GetSeriesAsync(KnownTitles.SimpsonsId, _token);

        Assert.NotEqual(true, series.RatingLists?.IsInTop250);
    }

    [Fact]
    public async Task PersonApi_ReturnsDeathDateOfDeceasedPerson()
    {
        var person = await Client.GetPersonAsync(KnownTitles.HeathLedgerId, _token);

        Assert.Equal("Хит Леджер", person.Name);
        Assert.Equal("1979-04-04", person.BirthDate);
        Assert.Equal("2008-01-22", person.DeathDate);
    }

    [Fact]
    public async Task PersonApi_LivingPersonHasNoDeathDate()
    {
        var person = await Client.GetPersonAsync(KnownTitles.KeanuReevesId, _token);

        Assert.Null(person.DeathDate);
    }

    [Fact]
    public async Task PersonApi_ReturnsFactsWithKinopoiskLinks()
    {
        var person = await Client.GetPersonAsync(KnownTitles.KeanuReevesId, _token);

        Assert.True(person.Facts?.Count > Constants.PersonBiographyFacts);
        Assert.All(person.Facts ?? [], fact => Assert.False(string.IsNullOrWhiteSpace(fact)));

        // Facts link to other Kinopoisk pages; the provider must strip that markup.
        Assert.Contains(person.Facts ?? [], fact => fact.Contains("<a ", StringComparison.Ordinal));
    }

    public void Dispose() => _services.Dispose();
}
