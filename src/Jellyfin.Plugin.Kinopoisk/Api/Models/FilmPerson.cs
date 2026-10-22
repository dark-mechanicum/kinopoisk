using System.Collections.Generic;
using System.Linq;
using Jellyfin.Plugin.Kinopoisk.Common;
using Jellyfin.Plugin.Kinopoisk.Extensions;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;

namespace Jellyfin.Plugin.Kinopoisk.Api.Models;

/// <summary>
/// A person, from the <c>/api/person/{id}/</c> endpoint, crew lists and search suggestions.
/// </summary>
public sealed record FilmPerson : BaseMetadata
{
    private readonly string? _name;

    /// <summary>Gets the localized name, falling back to the original one.</summary>
    public string? Name
    {
        get => string.IsNullOrWhiteSpace(_name) ? OriginalName : _name;
        init => _name = value;
    }

    /// <summary>Gets the original name.</summary>
    public string? OriginalName { get; init; }

    /// <summary>Gets the birth date (ISO).</summary>
    public string? BirthDate { get; init; }

    /// <summary>Gets the birth place.</summary>
    public string? BirthPlace { get; init; }

    /// <summary>Gets the death date (ISO); null for a living person.</summary>
    public string? DeathDate { get; init; }

    /// <summary>Gets the biography facts, as HTML fragments with links to other Kinopoisk pages.</summary>
    public IReadOnlyList<string>? Facts { get; init; }

    /// <summary>Gets the main photo set.</summary>
    public PersonImages? Img { get; init; }

    /// <summary>Gets the poster (search suggestions).</summary>
    public Image? Poster { get; init; }

    /// <summary>Gets additional photos.</summary>
    public IReadOnlyList<PersonGalleryItem>? Gallery { get; init; }

    /// <inheritdoc />
    public override RemoteSearchResult ToSearchResult()
    {
        var result = new RemoteSearchResult
        {
            Name = Name,
            SearchProviderName = Constants.ProviderName,
            ImageUrl = GetArtwork().Select(i => i.Url).FirstOrDefault(u => u is not null),
        };
        result.SetKinopoiskId(Id);
        return result;
    }

    /// <inheritdoc />
    public override IEnumerable<(ImageType Type, string? Url)> GetArtwork()
    {
        yield return (ImageType.Primary, Image.ToAbsoluteUrl(Img?.PosterMedium?.X2));
        yield return (ImageType.Primary, Poster?.Url);

        foreach (var photo in Gallery ?? [])
        {
            yield return (ImageType.Primary, Image.ToAbsoluteUrl(photo.BaseUrl, Image.DefaultSize));
        }
    }
}
