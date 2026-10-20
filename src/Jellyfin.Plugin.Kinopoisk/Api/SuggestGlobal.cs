using System.Collections.Generic;
using System.Text.Json.Serialization;
using Jellyfin.Plugin.Kinopoisk.Api.Models;

namespace Jellyfin.Plugin.Kinopoisk.Api;

/// <summary>
/// The best match of <c>SuggestSearch</c>: a film/series (has a title) or a person (has a name).
/// </summary>
internal sealed record SuggestGlobal
{
    /// <summary>Gets the Kinopoisk id.</summary>
    public int Id { get; init; }

    /// <summary>Gets the GraphQL type name.</summary>
    [JsonPropertyName("__typename")]
    public string? TypeName { get; init; }

    /// <summary>Gets the content UUID.</summary>
    public string? ContentId { get; init; }

    /// <summary>Gets the film title.</summary>
    public FilmTitle? Title { get; init; }

    /// <summary>Gets the production year.</summary>
    public int? ProductionYear { get; init; }

    /// <summary>Gets the release years.</summary>
    public IReadOnlyList<FilmYearsRange>? ReleaseYears { get; init; }

    /// <summary>Gets the gallery.</summary>
    public FilmGallery? Gallery { get; init; }

    /// <summary>Gets the person name.</summary>
    public string? Name { get; init; }

    /// <summary>Gets the person original name.</summary>
    public string? OriginalName { get; init; }

    /// <summary>Gets the person poster.</summary>
    public Image? Poster { get; init; }

    /// <summary>Gets a value indicating whether the match is a person.</summary>
    public bool IsPerson => TypeName == "Person";

    /// <summary>
    /// Converts a film/series match.
    /// </summary>
    /// <returns>The film.</returns>
    public FilmInfo ToFilm() => new()
    {
        Id = Id,
        TypeName = TypeName,
        ContentId = ContentId,
        Title = Title,
        ProductionYear = ProductionYear,
        ReleaseYears = ReleaseYears,
        Gallery = Gallery,
    };

    /// <summary>
    /// Converts a person match.
    /// </summary>
    /// <returns>The person.</returns>
    public FilmPerson ToPerson() => new() { Id = Id, Name = Name, OriginalName = OriginalName, Poster = Poster };
}
