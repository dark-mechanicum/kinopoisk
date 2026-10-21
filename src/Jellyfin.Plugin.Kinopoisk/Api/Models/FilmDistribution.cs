using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.Kinopoisk.Api.Models;

/// <summary>
/// The <c>distribution</c> node of <c>FilmBaseInfo</c> and <c>TvSeriesBaseInfo</c>.
/// </summary>
/// <param name="Originals">The original (streaming) release.</param>
/// <param name="DigitalRelease">The first digital release.</param>
/// <param name="RusRelease">The Russian cinema release.</param>
/// <param name="ReRelease">The Russian cinema re-release.</param>
public sealed record FilmDistribution(
    FilmReleaseList? Originals = null,
    FilmReleaseList? DigitalRelease = null,
    FilmReleaseList? RusRelease = null,
    FilmReleaseList? ReRelease = null)
{
    /// <summary>
    /// Gets the distinct company names: the network or streamer first, Russian distributors last.
    /// </summary>
    [JsonIgnore]
    public IEnumerable<string> CompanyNames
        => new[] { Originals, DigitalRelease, RusRelease, ReRelease }
            .SelectMany(list => list?.Items ?? [])
            .SelectMany(release => release.Companies ?? [])
            .Select(company => company.DisplayName?.Trim())
            .OfType<string>()
            .Where(name => name.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase);
}
