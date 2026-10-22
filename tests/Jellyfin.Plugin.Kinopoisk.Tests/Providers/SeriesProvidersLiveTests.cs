using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.Kinopoisk.Common;
using Jellyfin.Plugin.Kinopoisk.Configuration;
using Jellyfin.Plugin.Kinopoisk.Extensions;
using Jellyfin.Plugin.Kinopoisk.Providers;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using Xunit;

namespace Jellyfin.Plugin.Kinopoisk.Tests.Providers;

/// <summary>
/// Series and episode providers end to end, against the live API.
/// </summary>
public sealed class SeriesProvidersLiveTests : IDisposable
{
    private readonly LiveServices _services = new();
    private readonly CancellationToken _token = TestContext.Current.CancellationToken;

    [Fact]
    public async Task GetMetadata_ByName_FindsAndFillsSeries()
    {
        IRemoteMetadataProvider<Series, SeriesInfo> provider = _services.Create<SeriesMetadataProvider>();

        var result = await provider.GetMetadata(new SeriesInfo { Name = "Игра престолов", Year = 2011 }, _token);

        Assert.True(result.HasMetadata);
        Assert.True(result.Item.TryGetKinopoiskId(out var id));
        Assert.Equal(KnownTitles.GameOfThronesId, id);
        Assert.Equal("Игра престолов", result.Item.Name);
        Assert.Equal(2011, result.Item.ProductionYear);
        Assert.Equal("18+", result.Item.OfficialRating);
        Assert.False(string.IsNullOrWhiteSpace(result.Item.GetProviderId(Constants.ContentIdKey)));
        Assert.NotEmpty(result.People);
        Assert.Contains("HBO", result.Item.Studios);
        Assert.Contains("Фэнтези", result.Item.Genres);
    }

    [Fact]
    public async Task GetSearchResults_ByName_ReturnsOnlySeries()
    {
        IRemoteMetadataProvider<Series, SeriesInfo> provider = _services.Create<SeriesMetadataProvider>();

        var results = (await provider.GetSearchResults(new SeriesInfo { Name = "Игра престолов" }, _token)).ToList();

        Assert.Contains(results, r => r.TryGetKinopoiskId(out var id) && id == KnownTitles.GameOfThronesId);
        Assert.DoesNotContain(results, r => r.TryGetKinopoiskId(out var id) && id == KnownTitles.MatrixId);
    }

    [Fact]
    public async Task GetSearchResults_ByNameAndYear_PutsSeriesOfThatYearFirstAndSkipsFilms()
    {
        IRemoteMetadataProvider<Series, SeriesInfo> provider = _services.Create<SeriesMetadataProvider>();

        var results = (await provider.GetSearchResults(new SeriesInfo { Name = "Незнайка на Луне", Year = 1997 }, _token)).ToList();

        Assert.True(results[0].TryGetKinopoiskId(out var id));
        Assert.Equal(KnownTitles.NeznaykaNaLuneId, id);
        Assert.DoesNotContain(results, r => r.TryGetKinopoiskId(out var other) && other == KnownTitles.NeznaykaNaLune2Id);
    }

    [Fact]
    public async Task GetImages_ReturnsPostersAndBackdrops()
    {
        var provider = _services.Create<SeriesImageProvider>();
        var series = new Series();
        series.SetKinopoiskId(KnownTitles.GameOfThronesId);

        var images = (await provider.GetImages(series, _token)).ToList();

        Assert.Contains(images, i => i.Type == ImageType.Primary);
        Assert.Contains(images, i => i.Type == ImageType.Backdrop);
    }

    [Fact]
    public async Task GetMetadata_EndedSeries_HasStatusEndYearAndEpisodeLength()
    {
        var result = await GetSeriesMetadataAsync(_services, KnownTitles.GameOfThronesId);

        var series = result.Item;
        Assert.Equal(SeriesStatus.Ended, series.Status);
        Assert.Equal(new DateTime(2019, 7, 1, 12, 0, 0, DateTimeKind.Utc), series.EndDate);
        Assert.InRange(TimeSpan.FromTicks(series.RunTimeTicks ?? 0).TotalMinutes, 50, 60);
    }

    [Fact]
    public async Task GetMetadata_Miniseries_EndsInItsOnlyYear()
    {
        var result = await GetSeriesMetadataAsync(_services, KnownTitles.ChernobylId);

        Assert.Equal(SeriesStatus.Ended, result.Item.Status);
        Assert.Equal(2019, result.Item.ProductionYear);
        Assert.Equal(2019, result.Item.EndDate?.Year);
        Assert.InRange(TimeSpan.FromTicks(result.Item.RunTimeTicks ?? 0).TotalMinutes, 55, 65);
    }

    [Fact]
    public async Task GetMetadata_RunningSeries_IsContinuingWithoutEndDate()
    {
        var result = await GetSeriesMetadataAsync(_services, KnownTitles.SimpsonsId);

        Assert.Equal(SeriesStatus.Continuing, result.Item.Status);
        Assert.Null(result.Item.EndDate);
        Assert.InRange(TimeSpan.FromTicks(result.Item.RunTimeTicks ?? 0).TotalMinutes, 20, 25);
    }

    [Fact]
    public async Task GetMetadata_EndDateShowsEndYearInEveryTimeZone()
    {
        var result = await GetSeriesMetadataAsync(_services, KnownTitles.GameOfThronesId);
        var endDate = result.Item.EndDate!.Value;

        Assert.Equal(2019, TimeZoneInfo.ConvertTimeBySystemTimeZoneId(endDate, "Pacific/Pago_Pago").Year);
        Assert.Equal(2019, TimeZoneInfo.ConvertTimeBySystemTimeZoneId(endDate, "Pacific/Kiritimati").Year);
    }

    [Fact]
    public async Task GetMetadata_Top250Series_HasNoTagByDefault()
    {
        var result = await GetSeriesMetadataAsync(_services, KnownTitles.GameOfThronesId);

        Assert.DoesNotContain(Constants.Top250Tag, result.Item.Tags);
    }

    [Fact]
    public async Task GetMetadata_Top250Series_IsTaggedWhenEnabled()
    {
        using var services = new LiveServices(new PluginConfiguration { TagTop250 = true });

        var result = await GetSeriesMetadataAsync(services, KnownTitles.GameOfThronesId);

        Assert.Equal([Constants.Top250Tag], result.Item.Tags);
    }

    [Fact]
    public async Task GetMetadata_SeriesOutsideTop250_IsNotTaggedWhenEnabled()
    {
        using var services = new LiveServices(new PluginConfiguration { TagTop250 = true });

        var result = await GetSeriesMetadataAsync(services, KnownTitles.SimpsonsId);

        Assert.True(result.HasMetadata);
        Assert.Empty(result.Item.Tags);
    }

    [Fact]
    public async Task Episode_LeavesRuntimeAndStatusToOtherSources()
    {
        IRemoteMetadataProvider<Episode, EpisodeInfo> provider = _services.Create<EpisodeMetadataProvider>();
        var info = new EpisodeInfo { ParentIndexNumber = 8, IndexNumber = 6 };
        info.SeriesProviderIds[Constants.KinopoiskIdKey] = KnownTitles.GameOfThronesId.ToString(CultureInfo.InvariantCulture);

        var result = await provider.GetMetadata(info, _token);

        Assert.True(result.HasMetadata);
        Assert.Null(result.Item.RunTimeTicks);
        Assert.Null(result.Item.EndDate);
    }

    [Fact]
    public async Task Episode_FromLatestReleases_WhenSeriesHasNoSeasonContentIds()
    {
        IRemoteMetadataProvider<Episode, EpisodeInfo> provider = _services.Create<EpisodeMetadataProvider>();
        var info = new EpisodeInfo { ParentIndexNumber = 8, IndexNumber = 6 };
        info.SeriesProviderIds[Constants.KinopoiskIdKey] = KnownTitles.GameOfThronesId.ToString(CultureInfo.InvariantCulture);

        var result = await provider.GetMetadata(info, _token);

        Assert.True(result.HasMetadata);
        Assert.Equal("Железный Трон", result.Item.Name);
        Assert.Equal("The Iron Throne", result.Item.OriginalTitle);
        Assert.Equal(8, result.Item.ParentIndexNumber);
        Assert.Equal(6, result.Item.IndexNumber);
        Assert.Equal(2019, result.Item.ProductionYear);
    }

    [Theory]
    [Trait("Category", StreamingCatalog.Category)]
    [InlineData(1, 1)]
    [InlineData(1, 16)]
    [InlineData(2, 1)]
    public async Task Episode_FromItsSeason_WhenSeriesIsOnStreamingService(int season, int episode)
    {
        await StreamingCatalog.SkipUnlessAvailableAsync(_services.Client, _token);
        var provider = _services.Create<EpisodeMetadataProvider>();

        var found = await provider.FindEpisodeAsync(MetodEpisode(season, episode), _token);

        Assert.NotNull(found);
        Assert.Equal(episode, found.Number);
        Assert.Equal(season, found.Season?.Number);
        Assert.False(string.IsNullOrWhiteSpace(found.Synopsis));
    }

    [Fact]
    public async Task Episode_RecentEpisodeIsFoundWithOrWithoutStreamingCatalog()
    {
        // Season data when the streaming catalog is visible, otherwise the latest-episodes fallback.
        var provider = _services.Create<EpisodeMetadataProvider>();

        var found = await provider.FindEpisodeAsync(MetodEpisode(2, 1), _token);

        Assert.NotNull(found);
        Assert.Equal(1, found.Number);
        Assert.Equal(2, found.Season?.Number);
        Assert.True(found.Id > 0);
    }

    [Fact]
    public async Task Episode_WithoutSeriesId_ReturnsEmptyResult()
    {
        IRemoteMetadataProvider<Episode, EpisodeInfo> provider = _services.Create<EpisodeMetadataProvider>();

        var result = await provider.GetMetadata(new EpisodeInfo { ParentIndexNumber = 1, IndexNumber = 1 }, _token);

        Assert.False(result.HasMetadata);
    }

    public void Dispose() => _services.Dispose();

    private async Task<MetadataResult<Series>> GetSeriesMetadataAsync(LiveServices services, int seriesId)
    {
        IRemoteMetadataProvider<Series, SeriesInfo> provider = services.Create<SeriesMetadataProvider>();
        var info = new SeriesInfo();
        info.SetKinopoiskId(seriesId);

        var result = await provider.GetMetadata(info, _token);

        Assert.True(result.HasMetadata);
        return result;
    }

    private static EpisodeInfo MetodEpisode(int season, int episode)
    {
        var info = new EpisodeInfo { ParentIndexNumber = season, IndexNumber = episode };
        info.SeriesProviderIds[Constants.KinopoiskIdKey] = KnownTitles.MetodId.ToString(CultureInfo.InvariantCulture);
        info.SeriesProviderIds[Constants.ContentIdKey] = KnownTitles.MetodContentId;
        return info;
    }
}
