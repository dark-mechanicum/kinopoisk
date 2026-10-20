using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Jellyfin.Plugin.Kinopoisk.Common;
using Jellyfin.Plugin.Kinopoisk.Extensions;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;

namespace Jellyfin.Plugin.Kinopoisk.Api.Models;

/// <summary>
/// A film or a TV series as returned by <c>FilmBaseInfo</c>, <c>TvSeriesBaseInfo</c>,
/// <c>FilmPage</c> and search suggestions.
/// </summary>
public sealed record FilmInfo : BaseMetadata
{
    /// <summary>GraphQL type name of feature films; everything else is a kind of series.</summary>
    public const string FilmTypeName = "Film";

    /// <summary>Gets the content UUID (present for titles on the Kinopoisk streaming service).</summary>
    public string? ContentId { get; init; }

    /// <summary>Gets the GraphQL type: <c>Film</c>, <c>TvSeries</c>, <c>MiniSeries</c>, <c>TvShow</c>...</summary>
    [JsonPropertyName("__typename")]
    public string? TypeName { get; init; }

    /// <summary>Gets the title.</summary>
    public FilmTitle? Title { get; init; }

    /// <summary>Gets the ratings.</summary>
    public FilmRating? Rating { get; init; }

    /// <summary>Gets the curated gallery.</summary>
    public FilmGallery? Gallery { get; init; }

    /// <summary>Gets the actors.</summary>
    public FilmCrewMembers? Actors { get; init; }

    /// <summary>Gets the directors.</summary>
    public FilmCrewMembers? Directors { get; init; }

    /// <summary>Gets the writers.</summary>
    public FilmCrewMembers? Writers { get; init; }

    /// <summary>Gets the producers.</summary>
    public FilmCrewMembers? Producers { get; init; }

    /// <summary>Gets the composers.</summary>
    public FilmCrewMembers? Composers { get; init; }

    /// <summary>Gets the film editors.</summary>
    public FilmCrewMembers? FilmEditors { get; init; }

    /// <summary>Gets the production year.</summary>
    public int? ProductionYear { get; init; }

    /// <summary>Gets the release years (series).</summary>
    public IReadOnlyList<FilmYearsRange>? ReleaseYears { get; init; }

    /// <summary>Gets the tagline.</summary>
    public string? Tagline { get; init; }

    /// <summary>Gets the short description.</summary>
    public string? ShortDescription { get; init; }

    /// <summary>Gets the synopsis.</summary>
    public string? Synopsis { get; init; }

    /// <summary>Gets the genres.</summary>
    public IReadOnlyList<FilmNamedEntity> Genres { get; init; } = [];

    /// <summary>Gets the production countries.</summary>
    public IReadOnlyList<FilmNamedEntity> Countries { get; init; } = [];

    /// <summary>Gets the world premiere.</summary>
    public FilmPremiere? WorldPremiere { get; init; }

    /// <summary>Gets the age restriction.</summary>
    public FilmRestriction? Restriction { get; init; }

    /// <summary>Gets the season list (<c>FilmPage</c> of series available online).</summary>
    public FilmSeasonList? Seasons { get; init; }

    /// <summary>Gets the year to show: production year or the first release year.</summary>
    public int? Year => ProductionYear ?? (ReleaseYears is [var first, ..] ? first.Start : null);

    /// <summary>Gets a value indicating whether this is a feature film rather than a series.</summary>
    public bool IsFilm => TypeName is null or FilmTypeName;

    /// <summary>
    /// Maps a Kinopoisk image category onto a Jellyfin image type.
    /// </summary>
    /// <param name="type">The category.</param>
    /// <returns>The Jellyfin type.</returns>
    public static ImageType ToImageType(FilmImageType type) => type switch
    {
        FilmImageType.Wallpaper or FilmImageType.Still or FilmImageType.Screenshot => ImageType.Backdrop,
        _ => ImageType.Primary,
    };

    /// <inheritdoc />
    public override RemoteSearchResult ToSearchResult()
    {
        var name = Title?.Display;
        var original = Title?.Original;
        if (name is not null && !string.IsNullOrWhiteSpace(original) && !string.Equals(name, original, StringComparison.Ordinal))
        {
            name += $" ({original})";
        }

        var posters = Gallery?.Posters;
        var result = new RemoteSearchResult
        {
            Name = name,
            ProductionYear = Year,
            Overview = ShortDescription,
            SearchProviderName = Constants.ProviderName,
            ImageUrl = (posters?.HdVertical ?? posters?.KpVertical ?? posters?.Vertical ?? posters?.MarketingVertical)?.Url,
        };
        result.SetKinopoiskId(Id);
        result.SetContentId(ContentId);
        return result;
    }

    /// <inheritdoc />
    public override IEnumerable<(ImageType Type, string? Url)> GetArtwork()
    {
        var posters = Gallery?.Posters;
        yield return (ImageType.Primary, posters?.Vertical?.Url);
        yield return (ImageType.Primary, posters?.MarketingVertical?.Url);
        yield return (ImageType.Primary, posters?.KpVertical?.Url);
        yield return (ImageType.Logo, Gallery?.Logos?.Horizontal?.Url);
        yield return (ImageType.Backdrop, Gallery?.Covers?.Horizontal?.Url);
    }
}
