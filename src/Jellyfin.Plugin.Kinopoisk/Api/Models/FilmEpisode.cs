using System.Collections.Generic;
using Jellyfin.Plugin.Kinopoisk.Common;
using Jellyfin.Plugin.Kinopoisk.Extensions;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;

namespace Jellyfin.Plugin.Kinopoisk.Api.Models;

/// <summary>
/// A series episode, from <c>TvSeriesEpisodes</c> or <c>SerialStructureSeason</c>.
/// </summary>
public sealed record FilmEpisode : BaseMetadata
{
    /// <summary>Gets the episode number within the season.</summary>
    public int Number { get; init; }

    /// <summary>Gets the season.</summary>
    public FilmSeason? Season { get; init; }

    /// <summary>Gets the title.</summary>
    public FilmTitle? Title { get; init; }

    /// <summary>Gets the release date.</summary>
    public FilmIncompleteDate? ReleaseDate { get; init; }

    /// <summary>Gets the synopsis.</summary>
    public string? Synopsis { get; init; }

    /// <summary>Gets the cover.</summary>
    public Image? Cover { get; init; }

    /// <inheritdoc />
    public override RemoteSearchResult ToSearchResult()
    {
        var result = new RemoteSearchResult
        {
            Name = Title?.Display,
            IndexNumber = Number,
            ParentIndexNumber = Season?.Number,
            Overview = Synopsis,
            ImageUrl = Cover?.Url,
            SearchProviderName = Constants.ProviderName,
        };
        result.SetKinopoiskId(Id);
        return result;
    }

    /// <inheritdoc />
    public override IEnumerable<(ImageType Type, string? Url)> GetArtwork()
    {
        yield return (ImageType.Primary, Cover?.Url);
    }
}
