using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;

namespace Jellyfin.Plugin.Kinopoisk.Extensions;

/// <summary>
/// Turns a media file name into search keywords or an explicit Kinopoisk id, and Kinopoisk text into plain text.
/// </summary>
public static partial class StringExtensions
{
    private static readonly CultureInfo _russianCulture = CultureInfo.GetCultureInfo("ru-RU");

    /// <summary>
    /// Finds an explicit Kinopoisk id tag in a file or folder name: <c>kp301</c>, <c>kp-301</c>,
    /// <c>[kpid=301]</c>, <c>{kinopoisk_301}</c>... (case-insensitive, not part of a longer word or number).
    /// </summary>
    /// <param name="name">The file or folder name.</param>
    /// <param name="kinopoiskId">The id, when a valid tag is found.</param>
    /// <returns><c>true</c> when the name carries a positive Kinopoisk id.</returns>
    public static bool TryParseKinopoiskId(this string? name, out int kinopoiskId)
    {
        kinopoiskId = 0;
        if (string.IsNullOrWhiteSpace(name))
        {
            return false;
        }

        var match = KinopoiskIdTag().Match(name);
        return match.Success
            && int.TryParse(match.Groups["id"].ValueSpan, NumberStyles.None, CultureInfo.InvariantCulture, out kinopoiskId)
            && kinopoiskId > 0;
    }

    /// <summary>
    /// Formats a Kinopoisk genre the way other providers do: Kinopoisk sends «фантастика», Jellyfin shows «Фантастика».
    /// </summary>
    /// <param name="name">The genre name.</param>
    /// <returns>The name with a capital first letter.</returns>
    public static string ToGenreName(this string name)
        => string.IsNullOrEmpty(name) ? name : char.ToUpper(name[0], _russianCulture) + name[1..];

    /// <summary>
    /// Turns a Kinopoisk HTML fragment into plain text: «сестры <c>&lt;a href="/name/1/"&gt;Ким&lt;/a&gt;</c>» → «сестры Ким».
    /// </summary>
    /// <param name="html">The fragment.</param>
    /// <returns>The text without tags, with entities decoded and spaces collapsed.</returns>
    public static string ToPlainText(this string html)
        => Whitespace().Replace(WebUtility.HtmlDecode(HtmlTag().Replace(html, string.Empty)), " ").Trim();

    /// <summary>
    /// Builds (title, year) search candidates from a file path, best candidates first.
    /// Bracketed alternative titles (<c>Кровь машин [Blood Machines] (2019)</c>) come last.
    /// </summary>
    /// <param name="path">The file path or name.</param>
    /// <returns>The candidates.</returns>
    public static IReadOnlyList<(string Title, int? Year)> ParseFileName(this string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return [];
        }

        var fileName = Path.GetFileName(path);
        var parts = ByYear().Split(fileName);
        var candidates = new HashSet<(string Title, int? Year)>();
        var alternatives = new List<(string Title, int? Year)>();

        for (var index = parts.Length - 1; index >= 0; index--)
        {
            if (YearOnly().IsMatch(parts[index]))
            {
                var prefix = string.Join(' ', parts.Take(index));
                var year = int.Parse(parts[index], CultureInfo.InvariantCulture);
                alternatives.AddRange(BracketGroups().Matches(prefix).Select(m => (m.Groups["inner"].Value, (int?)year)));
                candidates.Add((BracketGroups().Replace(prefix, " "), year));
            }
        }

        candidates.Add((FileExtension().Replace(BracketGroups().Replace(fileName, " "), string.Empty), null));

        var result = new List<(string Title, int? Year)>();
        foreach (var (rawTitle, year) in candidates.OrderBy(x => x.Title.Length).ThenBy(x => x.Year is not null))
        {
            var title = AllButWords().Replace(rawTitle, " ").Trim();
            if (title.Length == 0)
            {
                continue;
            }

            AddDistinct(result, title, year);
            AddDistinct(result, title, null);

            title = StripReleaseTags(title);
            AddDistinct(result, title, year);

            title = LeadingDigits().Replace(title, string.Empty).Trim();
            AddDistinct(result, title, year);

            title = TrailingDigits().Replace(title, string.Empty).Trim();
            AddDistinct(result, title, year);
        }

        foreach (var (rawTitle, year) in alternatives)
        {
            var title = StripReleaseTags(AllButWords().Replace(rawTitle, " ").Trim());
            if (Letter().IsMatch(title))
            {
                AddDistinct(result, title, year);
            }
        }

        return result;
    }

    /// <summary>
    /// Cuts the first release tag and everything after it, which also drops a trailing release group.
    /// shortcut: a group is only removed when it follows a known tag; add a group list if bare "Title.GROUP.mkv" names show up.
    /// </summary>
    private static string StripReleaseTags(string title) => ReleaseTags().Replace(title, string.Empty).Trim();

    private static void AddDistinct(List<(string Title, int? Year)> result, string title, int? year)
    {
        if (title.Length > 0 && !result.Contains((title, year)))
        {
            result.Add((title, year));
        }
    }

    [GeneratedRegex(@"[^\w]+|[_\s]")]
    private static partial Regex AllButWords();

    [GeneratedRegex(@"^\d+")]
    private static partial Regex LeadingDigits();

    [GeneratedRegex(@"\d+$")]
    private static partial Regex TrailingDigits();

    [GeneratedRegex(@"\.\w+$")]
    private static partial Regex FileExtension();

    [GeneratedRegex(@"((?:19|20)\d{2})")]
    private static partial Regex ByYear();

    [GeneratedRegex(@"^(?:19|20)\d{2}$")]
    private static partial Regex YearOnly();

    [GeneratedRegex(@"\[(?<inner>[^\]]*)\]|\{(?<inner>[^}]*)\}")]
    private static partial Regex BracketGroups();

    [GeneratedRegex(@"[^\W\d_]")]
    private static partial Regex Letter();

    // Applied after AllButWords, so tag separators are spaces ("WEB DL", "DDP5 1").
    [GeneratedRegex(
        @"\b(?:2160p|1080p|720p|480p|4k|uhd|hdr(?:10)?|web\s?(?:dl(?:rip)?|rip)|hdtv(?:rip)?|bd\s?rip|br\s?rip|blu\s?ray|dvd\s?rip|remux|x26[45]|h\s?26[45]|hevc|avc|aac|ac3|dts|ddp?\s?\d(?:\s\d)?|open\s?matte|extended|unrated|director\s?s?\s?cut|rus|eng|ukr|\d+xrus|dub|mvo|avo)\b.*$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex ReleaseTags();

    [GeneratedRegex(@"(?<![\p{L}\p{N}])(?:kp|kinopoisk)(?:id)?[-_=]?(?<id>\d+)(?![\p{L}\p{N}])", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex KinopoiskIdTag();

    [GeneratedRegex(@"<[^>]*>")]
    private static partial Regex HtmlTag();

    [GeneratedRegex(@"\s+")]
    private static partial Regex Whitespace();
}
