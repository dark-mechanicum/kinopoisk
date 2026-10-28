using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Jellyfin.Data.Enums;
using Jellyfin.Plugin.Kinopoisk.Api.Models;
using Jellyfin.Plugin.Kinopoisk.Common;
using Jellyfin.Plugin.Kinopoisk.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;

namespace Jellyfin.Plugin.Kinopoisk.Extensions;

/// <summary>
/// Copies Kinopoisk metadata onto Jellyfin items.
/// </summary>
public static class MetadataResultExtensions
{
    private const string BiographyParagraphSeparator = "\n\n";

    // Jellyfin 12.4 has no cinematographer kind; such people show up under "Crew" with the role as caption.
    private static readonly (Func<FilmInfo, FilmCrewMembers?> Members, PersonKind Kind, string? Role)[] _credits =
    [
        (f => f.Directors, PersonKind.Director, null),
        (f => f.Actors, PersonKind.Actor, null),
        (f => f.Writers, PersonKind.Writer, null),
        (f => f.Producers, PersonKind.Producer, null),
        (f => f.Composers, PersonKind.Composer, null),
        (f => f.FilmEditors, PersonKind.Editor, null),
        (f => f.Operators, PersonKind.Unknown, "Оператор"),
        (f => f.Designers, PersonKind.Artist, "Художник"),
        (f => f.VoiceOverActors, PersonKind.Actor, "Озвучка"),
    ];

    /// <summary>
    /// Creates an empty result tagged with this provider.
    /// </summary>
    /// <typeparam name="T">The item type.</typeparam>
    /// <returns>The result.</returns>
    public static MetadataResult<T> CreateResult<T>()
        where T : BaseItem, new()
        => new()
        {
            Item = new T(),
            Provider = Constants.ProviderName,
            ResultLanguage = Constants.MetadataLanguage,
        };

    /// <summary>
    /// Fills a result from a film or series.
    /// </summary>
    /// <typeparam name="T">The item type.</typeparam>
    /// <param name="result">The result.</param>
    /// <param name="film">The film.</param>
    /// <param name="options">The plugin options.</param>
    public static void FillFrom<T>(this MetadataResult<T> result, FilmInfo film, IKinopoiskOptions options)
        where T : BaseItem
    {
        var item = result.Item;
        item.SetKinopoiskId(film.Id);
        item.SetContentId(film.ContentId);

        item.Name = film.Title?.Display;
        item.OriginalTitle = film.Title?.Original;
        item.ProductionYear = film.Year;
        item.PremiereDate = ParseDate(film.WorldPremiere?.IncompleteDate);
        item.CommunityRating = film.Rating?.GetCommunity(options.CommunityRating);
        item.CriticRating = film.Rating?.Critics;
        item.OfficialRating = film.Restriction?.GetOfficialRating(options.AgeRating);
        item.Tagline = film.Tagline;
        item.Overview = string.IsNullOrWhiteSpace(film.Synopsis) ? film.ShortDescription : film.Synopsis;

        foreach (var genre in film.Genres)
        {
            if (!string.IsNullOrWhiteSpace(genre.Name))
            {
                item.AddGenre(options.CapitalizeGenres ? genre.Name.ToGenreName() : genre.Name);
            }
        }

        item.ProductionLocations = [.. film.Countries.Select(c => c.Name).OfType<string>().Where(n => n.Length > 0)];

        foreach (var company in film.Distribution?.CompanyNames ?? [])
        {
            item.AddStudio(company);
        }

        foreach (var (members, kind, role) in _credits)
        {
            result.AddPeople(members(film), kind, role);
        }

        if (options.TagTop250 && film.RatingLists?.IsInTop250 == true)
        {
            item.Tags = [.. item.Tags.Append(Constants.Top250Tag).Distinct(StringComparer.OrdinalIgnoreCase)];
        }

        result.HasMetadata = true;
    }

    /// <summary>
    /// Fills what only a series has: the episode length, whether it still runs and the year it ended.
    /// </summary>
    /// <param name="series">The series.</param>
    /// <param name="film">The series as Kinopoisk returns it.</param>
    public static void FillSeriesRun(this Series series, FilmInfo film)
    {
        if (film.SeriesDuration is > 0 and var minutes)
        {
            series.RunTimeTicks = TimeSpan.FromMinutes(minutes).Ticks;
        }

        // The last range is the current run; a range without an end year means the series is still on air.
        if (film.ReleaseYears is [.., var lastRun])
        {
            series.Status = lastRun.End is null ? SeriesStatus.Continuing : SeriesStatus.Ended;
            series.EndDate = lastRun.End is { } endYear ? MidYear(endYear) : null;
        }
    }

    /// <summary>
    /// Fills a result from a person.
    /// </summary>
    /// <typeparam name="T">The item type.</typeparam>
    /// <param name="result">The result.</param>
    /// <param name="person">The person.</param>
    /// <param name="options">The plugin options.</param>
    public static void FillFrom<T>(this MetadataResult<T> result, FilmPerson person, IKinopoiskOptions options)
        where T : BaseItem
    {
        var item = result.Item;
        item.SetKinopoiskId(person.Id);
        item.Name = person.Name;
        item.OriginalTitle = person.OriginalName;
        item.PremiereDate = ParseDate(person.BirthDate);
        item.EndDate = ParseDate(person.DeathDate);
        if (!string.IsNullOrWhiteSpace(person.BirthPlace))
        {
            item.ProductionLocations = [person.BirthPlace];
        }

        if (options.IncludePersonBiography)
        {
            item.Overview = ToBiography(person.Facts);
        }

        result.HasMetadata = true;
    }

    /// <summary>
    /// Fills a result from an episode.
    /// </summary>
    /// <typeparam name="T">The item type.</typeparam>
    /// <param name="result">The result.</param>
    /// <param name="episode">The episode.</param>
    public static void FillFrom<T>(this MetadataResult<T> result, FilmEpisode episode)
        where T : BaseItem
    {
        var item = result.Item;
        item.SetKinopoiskId(episode.Id);
        item.Name = episode.Title?.Display;
        item.OriginalTitle = episode.Title?.Original;
        item.IndexNumber = episode.Number;
        item.ParentIndexNumber = episode.Season?.Number;
        item.Overview = episode.Synopsis;
        item.PremiereDate = ParseDate(episode.ReleaseDate);
        item.ProductionYear = item.PremiereDate?.Year;
        result.HasMetadata = true;
    }

    private static void AddPeople<T>(this MetadataResult<T> result, FilmCrewMembers? members, PersonKind kind, string? role)
        where T : BaseItem
    {
        foreach (var member in members?.Items ?? [])
        {
            var person = member.Person;
            if (person is null || string.IsNullOrWhiteSpace(person.Name))
            {
                continue;
            }

            var info = new PersonInfo { Name = person.Name, Type = kind, Role = role };
            info.SetKinopoiskId(person.Id);
            result.AddPerson(info);
        }
    }

    private static DateTime? ParseDate(FilmIncompleteDate? date) => ParseDate(date?.Date);

    // Kinopoisk only knows the end year. Mid-year noon UTC shows that year in every time zone; 1 January would be
    // the previous year west of Greenwich.
    private static DateTime MidYear(int year) => new(year, 7, 1, 12, 0, 0, DateTimeKind.Utc);

    private static string? ToBiography(IReadOnlyList<string>? facts)
    {
        var paragraphs = (facts ?? [])
            .Select(fact => fact.ToPlainText())
            .Where(fact => fact.Length > 0)
            .Take(Constants.PersonBiographyFacts);
        var biography = string.Join(BiographyParagraphSeparator, paragraphs);
        return biography.Length > 0 ? biography : null;
    }

    private static DateTime? ParseDate(string? date)
        => DateTime.TryParse(date, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var value)
            ? value
            : null;
}
