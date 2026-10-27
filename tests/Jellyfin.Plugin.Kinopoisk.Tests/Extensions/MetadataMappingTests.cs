using System;
using System.Linq;
using Jellyfin.Data.Enums;
using Jellyfin.Plugin.Kinopoisk.Api.Models;
using Jellyfin.Plugin.Kinopoisk.Common;
using Jellyfin.Plugin.Kinopoisk.Configuration;
using Jellyfin.Plugin.Kinopoisk.Extensions;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using Xunit;

namespace Jellyfin.Plugin.Kinopoisk.Tests.Extensions;

/// <summary>
/// Mapping rules that cannot be pinned with live data (edge cases the API rarely returns).
/// </summary>
public sealed class MetadataMappingTests
{
    private const string PlaceholderUrl =
        "https://cdnrmmhfbmozlswruhrk.svc.cdn.yandex.net/s3/kinopoisk-frontend/common-static/img/projector-logo/placeholder.svg";

    private static readonly PluginConfiguration _defaults = new();

    [Theory]
    [InlineData("r", null, "R")]
    [InlineData("pg13", "age12", "PG-13")]
    [InlineData("nc17", null, "NC-17")]
    [InlineData(null, "age16", "16+")]
    [InlineData("", "age0", "0+")]
    [InlineData(null, null, null)]
    public void OfficialRating_MpaaFirstThenRussianAge(string? mpaa, string? age, string? expected)
        => Assert.Equal(expected, new FilmRestriction(age, mpaa).GetOfficialRating(AgeRatingSystem.Mpaa));

    [Theory]
    [InlineData("r", "age18", "18+")]
    [InlineData("pg13", "age12", "12+")]
    [InlineData("g", null, "G")]
    [InlineData("pg", "", "PG")]
    [InlineData(null, "age6", "6+")]
    [InlineData(null, null, null)]
    public void OfficialRating_RussianAgeFirstThenMpaa(string? mpaa, string? age, string? expected)
        => Assert.Equal(expected, new FilmRestriction(age, mpaa).GetOfficialRating(AgeRatingSystem.Russian));

    [Theory]
    [InlineData(AgeRatingSystem.Mpaa)]
    [InlineData(AgeRatingSystem.Russian)]
    public void OfficialRating_UnknownValueFallsBackToTheOtherSystem(AgeRatingSystem preferred)
    {
        // Kinopoisk adds values without notice; an unknown one must never be stored as a rating Jellyfin cannot read.
        Assert.Equal("16+", new FilmRestriction("age16", "x").GetOfficialRating(preferred));
        Assert.Equal("PG", new FilmRestriction("age99", "pg").GetOfficialRating(preferred));
        Assert.Null(new FilmRestriction("age99", "x").GetOfficialRating(preferred));
    }

    [Fact]
    public void OfficialRating_EveryKinopoiskValueMapsToARatingJellyfinKnows()
    {
        // Jellyfin's "us" rating table, and the ages it reads as numbers.
        string[] jellyfinRatings = ["G", "PG", "PG-13", "R", "NC-17", "0+", "6+", "12+", "16+", "18+"];
        string[] mpaa = ["g", "pg", "pg13", "r", "nc17", "PG13", " R "];
        string[] ages = ["age0", "age6", "age12", "age16", "age18", "AGE18"];

        Assert.All(mpaa, m => Assert.Contains(new FilmRestriction(null, m).GetOfficialRating(AgeRatingSystem.Mpaa), jellyfinRatings));
        Assert.All(ages, a => Assert.Contains(new FilmRestriction(a, null).GetOfficialRating(AgeRatingSystem.Russian), jellyfinRatings));
    }

    [Fact]
    public void FillFrom_UsesTheConfiguredAgeRatingSystem()
    {
        var film = new FilmInfo { Id = 1, Restriction = new FilmRestriction("age18", "r") };
        var mpaa = MetadataResultExtensions.CreateResult<Movie>();
        var russian = MetadataResultExtensions.CreateResult<Movie>();

        mpaa.FillFrom(film, _defaults);
        russian.FillFrom(film, new PluginConfiguration { AgeRating = AgeRatingSystem.Russian });

        Assert.Equal("R", mpaa.Item.OfficialRating);
        Assert.Equal("18+", russian.Item.OfficialRating);
    }

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

        result.FillFrom(film, _defaults);

        var actor = Assert.Single(result.People);
        Assert.Equal("Актёр", actor.Name);
        Assert.Equal(PersonKind.Actor, actor.Type);
        Assert.Null(actor.Role);
    }

    [Fact]
    public void FillFrom_MapsOperatorsDesignersAndVoiceActorsWithRoles()
    {
        var film = new FilmInfo
        {
            Id = 1,
            Operators = Crew(new FilmPerson { Id = 2, Name = "Билл Поуп" }),
            Designers = Crew(new FilmPerson { Id = 3, Name = "Оуэн Патерсон" }),

            // Kinopoisk sends dubbing actors with name null and the Russian name as the original one.
            VoiceOverActors = Crew(new FilmPerson { Id = 4, OriginalName = "Всеволод Кузнецов" }),
        };
        var result = MetadataResultExtensions.CreateResult<Movie>();

        result.FillFrom(film, _defaults);

        Assert.Contains(result.People, p => p is { Name: "Билл Поуп", Type: PersonKind.Unknown, Role: "Оператор" });
        Assert.Contains(result.People, p => p is { Name: "Оуэн Патерсон", Type: PersonKind.Artist, Role: "Художник" });
        Assert.Contains(result.People, p => p is { Name: "Всеволод Кузнецов", Type: PersonKind.Actor, Role: "Озвучка" });
    }

    [Fact]
    public void FillFrom_CapitalizesGenres()
    {
        var result = MetadataResultExtensions.CreateResult<Movie>();

        result.FillFrom(new FilmInfo { Id = 1, Genres = [new FilmNamedEntity("фантастика"), new FilmNamedEntity("для взрослых")] }, _defaults);

        Assert.Equal(["Фантастика", "Для взрослых"], result.Item.Genres);
    }

    [Fact]
    public void FillFrom_KeepsGenreCaseWhenDisabled()
    {
        var result = MetadataResultExtensions.CreateResult<Movie>();

        result.FillFrom(new FilmInfo { Id = 1, Genres = [new FilmNamedEntity("фантастика")] }, new PluginConfiguration { CapitalizeGenres = false });

        Assert.Equal(["фантастика"], result.Item.Genres);
    }

    [Fact]
    public void FillFrom_AddsDistributorsAsStudiosOnce()
    {
        var film = new FilmInfo
        {
            Id = 1,
            Distribution = new FilmDistribution(
                Originals: Releases("HBO"),
                RusRelease: Releases("Каро-Премьер", " ", null),
                ReRelease: Releases("hbo", "Маурис Филм")),
        };
        var result = MetadataResultExtensions.CreateResult<Movie>();

        result.FillFrom(film, _defaults);

        Assert.Equal(["HBO", "Каро-Премьер", "Маурис Филм"], result.Item.Studios);
    }

    [Theory]
    [InlineData(RatingSource.Kinopoisk, true, true, 8.5f)]
    [InlineData(RatingSource.Imdb, true, true, 8.7f)]
    [InlineData(RatingSource.Imdb, true, false, 8.5f)]
    [InlineData(RatingSource.Kinopoisk, false, true, 8.7f)]
    [InlineData(RatingSource.Kinopoisk, false, false, null)]
    public void CommunityRating_PrefersChosenSourceAndFallsBack(RatingSource source, bool kinopoiskActive, bool imdbActive, float? expected)
    {
        var rating = new FilmRating
        {
            Kinopoisk = new FilmRatingValue { Value = 8.5f, IsActive = kinopoiskActive },
            Imdb = new FilmRatingValue { Value = 8.7f, IsActive = imdbActive },
        };

        Assert.Equal(expected, rating.GetCommunity(source));
    }

    [Fact]
    public void FillFrom_IgnoresMissingContentId()
    {
        var result = MetadataResultExtensions.CreateResult<Movie>();

        result.FillFrom(new FilmInfo { Id = 5, ContentId = string.Empty }, _defaults);

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

        Assert.Equal(7.5f, rating.GetCommunity(RatingSource.Kinopoisk));
    }

    [Theory]
    [InlineData(PlaceholderUrl, true)]
    [InlineData("https://st.kp.yandex.net/images/no-poster.gif", true)]
    [InlineData("https://host/logo.SVG", true)]
    [InlineData("https://avatars.mds.yandex.net/get-kinopoisk-image/1/abc/576x", false)]
    [InlineData(null, false)]
    public void IsPlaceholder_DetectsKinopoiskPlaceholders(string? url, bool expected)
        => Assert.Equal(expected, Image.IsPlaceholder(url));

    [Fact]
    public void FilmPerson_PlaceholderPhotoGivesNoArtworkAndNoSearchImage()
    {
        var person = new FilmPerson
        {
            Id = 544474,
            Name = "Холли Барио",
            Img = new PersonImages(new PersonScaledImage(PlaceholderUrl, PlaceholderUrl)),
        };

        Assert.DoesNotContain(person.GetArtwork(), i => i.Url is not null);
        Assert.Null(person.ToSearchResult().ImageUrl);
    }

    [Fact]
    public void FilmInfo_PlaceholderPosterGivesNoSearchImage()
    {
        var film = new FilmInfo
        {
            Id = 1,
            Gallery = new FilmGallery(Posters: new FilmGalleryImages { Vertical = new Image { AvatarsUrl = "//st.kp.yandex.net/images/no-poster.gif" } }),
        };

        Assert.Null(film.ToSearchResult().ImageUrl);
        Assert.DoesNotContain(film.GetArtwork(), i => i.Url is not null);
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

    [Fact]
    public void FillSeriesRun_UsesTheLastRunForStatusAndEndYear()
    {
        // A revived series: Kinopoisk lists every run, the last one decides.
        var series = new Series();

        series.FillSeriesRun(new FilmInfo { ReleaseYears = [new FilmYearsRange(1990, 1991), new FilmYearsRange(2017, null)] });

        Assert.Equal(SeriesStatus.Continuing, series.Status);
        Assert.Null(series.EndDate);
    }

    [Fact]
    public void FillSeriesRun_WithoutReleaseYearsLeavesStatusAndEndDateEmpty()
    {
        var series = new Series();

        series.FillSeriesRun(new FilmInfo { SeriesDuration = 0 });

        Assert.Null(series.Status);
        Assert.Null(series.EndDate);
        Assert.Null(series.RunTimeTicks);
    }

    [Fact]
    public void FillFrom_Top250TagIsAddedOnceNextToExistingTags()
    {
        var film = new FilmInfo { Id = 1, RatingLists = new FilmRatingLists(new FilmListEntry("top250", 33)) };
        var result = MetadataResultExtensions.CreateResult<Movie>();
        result.Item.Tags = ["Избранное", Constants.Top250Tag];

        result.FillFrom(film, new PluginConfiguration { TagTop250 = true });

        Assert.Equal(["Избранное", Constants.Top250Tag], result.Item.Tags);
    }

    [Fact]
    public void FilmRatingLists_PositionZeroIsNotInTop250()
        => Assert.False(new FilmRatingLists(new FilmListEntry("top250", 0)).IsInTop250);

    [Fact]
    public void FillFrom_PersonBiographySkipsEmptyFactsAndKeepsTheLimit()
    {
        var facts = new[] { " ", "<b></b>" }.Concat(Enumerable.Range(1, 15).Select(i => $"Факт {i}")).ToList();
        var result = MetadataResultExtensions.CreateResult<Person>();

        result.FillFrom(new FilmPerson { Id = 1, Facts = facts }, new PluginConfiguration { IncludePersonBiography = true });

        var paragraphs = result.Item.Overview!.Split("\n\n");
        Assert.Equal(Constants.PersonBiographyFacts, paragraphs.Length);
        Assert.Equal("Факт 1", paragraphs[0]);
    }

    [Fact]
    public void FillFrom_PersonWithUnparsableDeathDateHasNoEndDate()
    {
        var result = MetadataResultExtensions.CreateResult<Person>();

        result.FillFrom(new FilmPerson { Id = 1, DeathDate = "unknown" }, _defaults);

        Assert.Null(result.Item.EndDate);
    }

    private static FilmCrewMembers Crew(FilmPerson person) => new([new FilmCrewMember(person)]);

    private static FilmReleaseList Releases(params string?[] companies)
        => new([new FilmRelease([.. companies.Select(c => new FilmCompany(c))])]);
}
