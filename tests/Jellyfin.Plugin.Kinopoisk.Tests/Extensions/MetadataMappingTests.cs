using System;
using System.Linq;
using Jellyfin.Data.Enums;
using Jellyfin.Plugin.Kinopoisk.Api.Models;
using Jellyfin.Plugin.Kinopoisk.Common;
using Jellyfin.Plugin.Kinopoisk.Extensions;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Providers;
using Xunit;

namespace Jellyfin.Plugin.Kinopoisk.Tests.Extensions;

/// <summary>
/// Mapping rules that cannot be pinned with live data (edge cases the API rarely returns).
/// </summary>
public sealed class MetadataMappingTests
{
    [Theory]
    [InlineData("r", null, "R")]
    [InlineData("pg13", "age12", "PG-13")]
    [InlineData("nc17", null, "NC-17")]
    [InlineData(null, "age16", "16+")]
    [InlineData("", "age0", "0+")]
    [InlineData(null, null, null)]
    public void OfficialRating_PrefersMpaaThenRussianAge(string? mpaa, string? age, string? expected)
        => Assert.Equal(expected, new FilmRestriction(age, mpaa).OfficialRating);

    [Fact]
    public void FillFrom_SkipsCrewWithoutNameButKeepsTheRest()
    {
        var film = new FilmInfo
        {
            Id = 1,
            Actors = new FilmCrewMembers([
                new FilmCrewMember(new FilmPerson { Id = 2 }),
                new FilmCrewMember(null),
                new FilmCrewMember(new FilmPerson { Id = 3, Name = "Актёр" }),
            ]),
        };
        var result = MetadataResultExtensions.CreateResult<Movie>();

        result.FillFrom(film);

        var actor = Assert.Single(result.People);
        Assert.Equal("Актёр", actor.Name);
        Assert.Equal(PersonKind.Actor, actor.Type);
    }

    [Fact]
    public void FillFrom_IgnoresMissingContentId()
    {
        var result = MetadataResultExtensions.CreateResult<Movie>();

        result.FillFrom(new FilmInfo { Id = 5, ContentId = string.Empty });

        Assert.False(result.Item.TryGetContentId(out _));
        Assert.True(result.Item.TryGetKinopoiskId(out var id) && id == 5);
    }

    [Fact]
    public void FillFrom_EpisodeUsesLocalizedTitleAndReleaseDate()
    {
        var episode = new FilmEpisode
        {
            Id = 9,
            Number = 3,
            Season = new FilmSeason(2),
            Title = new FilmTitle(Localized: "Серия", Original: "Episode"),
            ReleaseDate = new FilmIncompleteDate("DAY", "2020-01-31"),
        };
        var result = MetadataResultExtensions.CreateResult<Episode>();

        result.FillFrom(episode);

        Assert.Equal("Серия", result.Item.Name);
        Assert.Equal(2, result.Item.ParentIndexNumber);
        Assert.Equal(3, result.Item.IndexNumber);
        Assert.Equal(new DateTime(2020, 1, 31, 0, 0, 0, DateTimeKind.Utc), result.Item.PremiereDate);
        Assert.Equal(2020, result.Item.ProductionYear);
    }

    [Fact]
    public void Rating_IgnoresInactiveKinopoiskValue()
    {
        var rating = new FilmRating
        {
            Kinopoisk = new FilmRatingValue { Value = 5, IsActive = false },
            Imdb = new FilmRatingValue { Value = 7.5f, IsActive = true },
        };

        Assert.Equal(7.5f, rating.Community);
    }

    [Theory]
    [InlineData("//host/path", "https://host/path/576x")]
    [InlineData("https://host/path", "https://host/path/576x")]
    [InlineData("https://host/path/", "https://host/path/576x")]
    [InlineData("", null)]
    [InlineData(null, null)]
    public void ImageUrl_IsAlwaysAbsoluteHttps(string? avatarsUrl, string? expected)
        => Assert.Equal(expected, new Image { AvatarsUrl = avatarsUrl }.Url);

    [Fact]
    public void ProviderIds_RejectNonPositiveAndGarbageIds()
    {
        var info = new MovieInfo();
        info.SetKinopoiskId(0);
        Assert.False(info.TryGetKinopoiskId(out _));

        info.ProviderIds[Constants.KinopoiskIdKey] = "tt0133093";
        Assert.False(info.TryGetKinopoiskId(out _));

        info.SetKinopoiskId(301);
        Assert.True(info.TryGetKinopoiskId(out var id));
        Assert.Equal(301, id);
    }

    [Fact]
    public void SearchResult_CombinesRussianAndOriginalTitle()
    {
        var film = new FilmInfo { Id = 1, Title = new FilmTitle("Матрица", "The Matrix"), ProductionYear = 1999 };
        var sameTitle = new FilmInfo { Id = 2, Title = new FilmTitle("Up", "Up") };

        Assert.Equal("Матрица (The Matrix)", film.ToSearchResult().Name);
        Assert.Equal("Up", sameTitle.ToSearchResult().Name);
        Assert.Equal(Constants.ProviderName, film.ToSearchResult().SearchProviderName);
        Assert.DoesNotContain(new FilmInfo().GetArtwork(), i => i.Url is not null);
    }
}
