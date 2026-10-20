using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Jellyfin.Plugin.Kinopoisk.Extensions;

/// <summary>
/// Turns a media file name into search keywords.
/// </summary>
public static partial class StringExtensions
{
    /// <summary>
    /// Builds (title, year) search candidates from a file path, best candidates first.
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

        for (var index = parts.Length - 1; index >= 0; index--)
        {
            if (YearOnly().IsMatch(parts[index]))
            {
                candidates.Add((string.Join(' ', parts.Take(index)), int.Parse(parts[index], CultureInfo.InvariantCulture)));
            }
        }

        candidates.Add((FileExtension().Replace(fileName, string.Empty), null));

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

            title = VideoFormats().Replace(title, string.Empty).Trim();
            AddDistinct(result, title, year);

            title = LeadingDigits().Replace(title, string.Empty).Trim();
            AddDistinct(result, title, year);

            title = TrailingDigits().Replace(title, string.Empty).Trim();
            AddDistinct(result, title, year);
        }

        return result;
    }

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

    [GeneratedRegex("EXTENDED|BDRIP|BRRIP|DVDRIP", RegexOptions.IgnoreCase)]
    private static partial Regex VideoFormats();
}
