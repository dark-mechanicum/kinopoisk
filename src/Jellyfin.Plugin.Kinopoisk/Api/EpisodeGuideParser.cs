using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using Jellyfin.Plugin.Kinopoisk.Api.Models;
using Jellyfin.Plugin.Kinopoisk.Extensions;

namespace Jellyfin.Plugin.Kinopoisk.Api;

/// <summary>
/// Reads the server-rendered episode list of <c>kinopoisk.ru/film/{id}/episodes/</c>: every season and episode with
/// titles and release dates, but no episode ids or synopses.
/// </summary>
internal static partial class EpisodeGuideParser
{
    private const string DayAccuracy = "DAY";
    private const string MonthAccuracy = "MONTH";
    private const string IsoDateFormat = "yyyy-MM-dd";
    private const string LoginPageMarker = "sso.passport.yandex.ru";

    // Genitive month names as the page prints them («23 марта 2024»); fixed here so parsing never depends on the
    // cultures installed on the server.
    private static readonly string[] _months =
    [
        "января", "февраля", "марта", "апреля", "мая", "июня",
        "июля", "августа", "сентября", "октября", "ноября", "декабря",
    ];

    /// <summary>
    /// Parses the episode list.
    /// </summary>
    /// <param name="html">The page.</param>
    /// <returns>The episodes in page order; empty when the page has no episode list.</returns>
    public static IReadOnlyList<FilmEpisode> Parse(string html)
    {
        var episodes = new List<FilmEpisode>();
        FilmSeason? season = null;
        foreach (Match match in SeasonOrEpisode().Matches(html))
        {
            if (match.Groups["season"].Success)
            {
                season = new FilmSeason(int.Parse(match.Groups["season"].ValueSpan, CultureInfo.InvariantCulture));
            }
            else if (season is not null)
            {
                episodes.Add(ToEpisode(match, season));
            }
        }

        return episodes;
    }

    /// <summary>
    /// Tells whether Kinopoisk sent the Yandex login redirect instead of the page.
    /// </summary>
    /// <param name="html">The page.</param>
    /// <returns><c>true</c> for the login redirect page.</returns>
    public static bool IsLoginPage(string html) => html.Contains(LoginPageMarker, StringComparison.Ordinal);

    /// <summary>
    /// Reads a release date cell: <c>23 марта 2024</c>, <c>октября 2026</c> (month only) or empty.
    /// </summary>
    /// <param name="text">The cell text.</param>
    /// <returns>The date, or null when the cell has none.</returns>
    internal static FilmIncompleteDate? ParseReleaseDate(string text)
    {
        var match = ReleaseDate().Match(text.ToPlainText());
        if (!match.Success)
        {
            return null;
        }

        var month = Array.IndexOf(_months, match.Groups["month"].Value) + 1;
        var hasDay = match.Groups["day"].Success;
        var day = hasDay ? int.Parse(match.Groups["day"].ValueSpan, CultureInfo.InvariantCulture) : 1;
        var year = int.Parse(match.Groups["year"].ValueSpan, CultureInfo.InvariantCulture);
        if (month == 0 || day > DateTime.DaysInMonth(year, month))
        {
            return null;
        }

        var date = new DateOnly(year, month, day).ToString(IsoDateFormat, CultureInfo.InvariantCulture);
        return new FilmIncompleteDate(hasDay ? DayAccuracy : MonthAccuracy, date);
    }

    private static FilmEpisode ToEpisode(Match match, FilmSeason season)
        => new()
        {
            Number = int.Parse(match.Groups["number"].ValueSpan, CultureInfo.InvariantCulture),
            Season = season,
            Title = new FilmTitle(ToTitle(match.Groups["title"].Value), ToTitle(match.Groups["original"].Value)),
            ReleaseDate = ParseReleaseDate(match.Groups["date"].Value),
        };

    // Kinopoisk fills unknown titles with «Эпизод #3.24» / «Episode #3.1»; they are no real titles.
    private static string? ToTitle(string html)
    {
        var title = html.ToPlainText();
        return title.Length == 0 || PlaceholderTitle().IsMatch(title) ? null : title;
    }

    [GeneratedRegex(
        """<a name="s(?<season>\d+)"></a>|Эпизод (?<number>\d+)</span><br>\s*<h1[^>]*><b>(?<title>[^<]*)</b></h1>\s*(?:<span class='episodesOriginalName'>(?<original>[^<]*)</span>)?\s*</td>\s*<td[^>]*>(?<date>[^<]*)</td>""",
        RegexOptions.CultureInvariant)]
    private static partial Regex SeasonOrEpisode();

    [GeneratedRegex(@"^(?:(?<day>\d{1,2})\s+)?(?<month>\p{L}+)\s+(?<year>\d{4})$", RegexOptions.CultureInvariant)]
    private static partial Regex ReleaseDate();

    [GeneratedRegex(@"^(?:Эпизод|Episode) #\d+\.\d+$", RegexOptions.CultureInvariant)]
    private static partial Regex PlaceholderTitle();
}
